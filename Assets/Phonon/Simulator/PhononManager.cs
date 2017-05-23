//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using System;

namespace Phonon
{
    public class PhononManager : MonoBehaviour
    {
        private void Awake()
        {
            bool initializeRenderer = true;
            Initialize(initializeRenderer);
            phononContainer.Initialize(initializeRenderer, this);
        }

        private void OnDestroy()
        {
            Destroy();
            phononContainer.Destroy();
        }

        private void Update()
        {
            bool setValue = updateComponents;
            AudioListener(setValue);
            PhononListener(setValue);
        }

        // Initializes Phonon Manager, in particular various Phonon API handles contained within Phonon Manager.
        // Initialize will be performed only once despite repeated calls to Initialize without calls to Destroy.
        public void Initialize(bool initializeRenderer)
        {
            if (isInitialized)
                return;

            isInitialized = true;

            bool setValue = true;
            AudioListener(setValue);
            PhononListener(setValue);
            PhononStaticListener();
            CustomPhononSettings();
            CustomSpeakerLayout();
        }

        // Destroys Phonon Manager.
        public void Destroy()
        {
            audioListener = null;
            phononListener = null;
            phononStaticListener = null;
            customSpeakerLayout = null;
            customPhononSettings = null;

            isSetPhononStaticListener = false;
            isSetCustomSpeakerLayout = false;
            isSetCustomPhononSettings = false;
            isInitialized = false;
        }

        // Returns Phonon Manager Container.
        public PhononManagerContainer PhononManagerContainer()
        {
            return phononContainer;
        }

        // Returns Simulation Settings.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public SimulationSettings SimulationSettings()
        {
            SceneType rayTracer = RayTracerOption();
            float simDuration = simulationValue.Duration;
            int simAmbisonicsOrder = simulationValue.AmbisonicsOrder;
            int simMaxSources = simulationValue.MaxSources;

            int simRays = 0;
            int simSecondaryRays = 0;
            int simBounces = 0;

            bool editorInEditMode = false;
#if UNITY_EDITOR 
            editorInEditMode = Application.isEditor && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#endif
            //When in edit mode use baked settings, when in play mode or standalone application mode use realtime setttings.
            if (editorInEditMode)
            {
                simRays = simulationValue.BakeRays;
                simSecondaryRays = simulationValue.BakeSecondaryRays;
                simBounces = simulationValue.BakeBounces;
            }
            else
            {
                simRays = simulationValue.RealtimeRays;
                simSecondaryRays = simulationValue.RealtimeSecondaryRays;
                simBounces = simulationValue.RealtimeBounces;
            }

            SimulationSettings simulationSettings = new SimulationSettings
            {
                sceneType               = rayTracer,
                rays                    = simRays,
                secondaryRays           = simSecondaryRays,
                bounces                 = simBounces,
                irDuration              = simDuration,
                ambisonicsOrder         = simAmbisonicsOrder,
                maxConvolutionSources   = simMaxSources,
            };

            return simulationSettings;
        }

        // Returns Global Context.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public GlobalContext GlobalContext()
        {
            GlobalContext globalContext;
            globalContext.logCallback = IntPtr.Zero;
            globalContext.allocateCallback = IntPtr.Zero;
            globalContext.freeCallback = IntPtr.Zero;

            return globalContext;
        }

        // Returns Rendering Settings.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public RenderingSettings RenderingSettings()
        {
            int numBuffers, frameSize;
            RenderingSettings renderingSettings;

            AudioSettings.GetDSPBufferSize(out frameSize, out numBuffers);
            renderingSettings.samplingRate = AudioSettings.outputSampleRate;
            renderingSettings.frameSize = frameSize;
            renderingSettings.convolutionOption = ConvolutionOption();

            return renderingSettings;
        }

        // Returns Audio Format.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public AudioFormat AudioFormat()
        {
            AudioSpeakerMode projectSpeakerMode = AudioSettings.GetConfiguration().speakerMode;
            AudioSpeakerMode driverSpeakerMode = AudioSettings.driverCapabilities;
            AudioSpeakerMode minSpeakerMode;

            // NOTE: Prologic mode is not supported. Revert to stereo.
            if ((projectSpeakerMode == AudioSpeakerMode.Prologic) && (driverSpeakerMode == AudioSpeakerMode.Prologic))
                minSpeakerMode = AudioSpeakerMode.Stereo;
            else
                minSpeakerMode = (projectSpeakerMode < driverSpeakerMode) ? projectSpeakerMode : driverSpeakerMode;

            AudioFormat audioFormat;
            audioFormat.channelLayoutType = ChannelLayoutType.Speakers;
            audioFormat.speakerDirections = null;
            audioFormat.ambisonicsOrder = -1;
            audioFormat.ambisonicsOrdering = AmbisonicsOrdering.ACN;
            audioFormat.ambisonicsNormalization = AmbisonicsNormalization.N3D;
            audioFormat.channelOrder = ChannelOrder.Interleaved;

            switch (minSpeakerMode)
            {
                case AudioSpeakerMode.Mono:
                    audioFormat.channelLayout = ChannelLayout.Mono;
                    audioFormat.numSpeakers = 1;
                    break;
                case AudioSpeakerMode.Stereo:
                    audioFormat.channelLayout = ChannelLayout.Stereo;
                    audioFormat.numSpeakers = 2;
                    break;
                case AudioSpeakerMode.Quad:
                    audioFormat.channelLayout = ChannelLayout.Quadraphonic;
                    audioFormat.numSpeakers = 4;
                    break;
                case AudioSpeakerMode.Mode5point1:
                    audioFormat.channelLayout = ChannelLayout.FivePointOne;
                    audioFormat.numSpeakers = 6;
                    break;
                case AudioSpeakerMode.Mode7point1:
                    audioFormat.channelLayout = ChannelLayout.SevenPointOne;
                    audioFormat.numSpeakers = 8;
                    break;
                default:
                    Debug.LogWarning("Surround and Prologic mode is not supported. Revert to stereo");
                    audioFormat.channelLayout = ChannelLayout.Stereo;
                    audioFormat.numSpeakers = 2;
                    break;
            }

            CustomSpeakerLayout speakerLayout = CustomSpeakerLayout();
            if ((speakerLayout != null) && (speakerLayout.speakerPositions.Length == audioFormat.numSpeakers))
            {
                audioFormat.channelLayout = ChannelLayout.Custom;
                audioFormat.speakerDirections = new Vector3[audioFormat.numSpeakers];
                for (int i = 0; i < speakerLayout.speakerPositions.Length; ++i)
                {
                    audioFormat.speakerDirections[i].x = speakerLayout.speakerPositions[i].x;
                    audioFormat.speakerDirections[i].y = speakerLayout.speakerPositions[i].y;
                    audioFormat.speakerDirections[i].z = speakerLayout.speakerPositions[i].z;
                }
            }

            return audioFormat;
        }

        // Returns Scene Type.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public SceneType RayTracerOption()
        {
            CustomPhononSettings customSettings = CustomPhononSettings();
            if (customSettings)
                return customSettings.rayTracerOption;

            return SceneType.Phonon;
        }

        // Returns Convolution Option.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public ConvolutionOption ConvolutionOption()
        {
            CustomPhononSettings customSettings = CustomPhononSettings();
            if (customSettings)
                return customSettings.convolutionOption;

            return Phonon.ConvolutionOption.Phonon;
        }

        // Returns Compute Device Type. 
        // Safe to call this function without Initialize being called on Phonon Manager.
        public ComputeDeviceType ComputeDeviceSettings(out int numComputeUnits, out bool useOpenCL)
        {
            CustomPhononSettings customSettings = CustomPhononSettings();
            if (customSettings)
            {
                numComputeUnits = customSettings.numComputeUnits;
                useOpenCL = customSettings.useOpenCL;
                return customSettings.computeDeviceOption;
            }

            numComputeUnits = 0;
            useOpenCL = false;
            return ComputeDeviceType.CPU;
        }

        // Sets Audio Listener. This function is called when Phonon Manager is initialized. 
        // Further, this functionis also called every frame update to keep latest version of Audio Listener. 
        // Returns Audio Listener. The value is null if Phonon Manager has not been initialized.
        // To reduce overhead due to FindObjectOfType call, the value can potentially be cached. Audio Listener cannot
        // be changed in that case. See PhononStaticListener() on caching the value properly.
        public AudioListener AudioListener(bool setValue = false)
        {
            if (setValue)
                audioListener = FindObjectOfType<AudioListener>();

            return audioListener;
        }

        // Sets Phonon Listener. This function is called when Phonon Manager is initialized. 
        // Further, this functionis also called every frame update to keep latest version of Phonon Listener. 
        // Returns Phonon Listener. The value is null if Phonon Manager has not been initialized.
        // To reduce overhead due to FindObjectOfType call, the value can potentially be cached. Audio Listener cannot
        // be changed in that case. See PhononStaticListener() on caching the value properly.
        public PhononListener PhononListener(bool setValue = false)
        {
            if (setValue)
                phononListener = FindObjectOfType<PhononListener>();

            return phononListener;
        }

        // Query Phonon Static Listener. Result is cached.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public PhononStaticListener PhononStaticListener()
        {
            if (!isSetPhononStaticListener && phononStaticListener == null)
            {
                phononStaticListener = FindObjectOfType<PhononStaticListener>();
                isSetPhononStaticListener = true;
            }

            return phononStaticListener;
        }

        // Query Custom Speaker Layout. Result is cached.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public CustomSpeakerLayout CustomSpeakerLayout()
        {
            if (!isSetCustomSpeakerLayout && customSpeakerLayout == null)
            {
                customSpeakerLayout = FindObjectOfType<CustomSpeakerLayout>();
                isSetCustomSpeakerLayout = true;
            }

            return customSpeakerLayout;
        }

        // Query Custom Phonon Settings. Result is cached.
        // Safe to call this function without Initialize being called on Phonon Manager.
        public CustomPhononSettings CustomPhononSettings()
        {
            if (!isSetCustomPhononSettings && customPhononSettings == null)
            {
                customPhononSettings = FindObjectOfType<CustomPhononSettings>();
                isSetCustomPhononSettings = true;
            }

            return customPhononSettings;
        }

        // Exports Unity Scene and saves it in a phononscene file.
        public void ExportScene()
        {
            Scene exportScene = new Scene();
            ComputeDevice exportComputeDevice = new ComputeDevice();
            try
            {
                exportScene.Export(exportComputeDevice, SimulationSettings(), materialValue, GlobalContext());
            }
            catch (Exception e)
            {
                Debug.LogError("Phonon Geometry not attached. " + e.Message);
            }
        }

        // Exports Unity Scene and saves it in an OBJ file.
        public void DumpScene()
        {
            Scene exportScene = new Scene();
            ComputeDevice exportComputeDevice = new ComputeDevice();
            try
            {
                exportScene.DumpToObj(exportComputeDevice, SimulationSettings(), materialValue, GlobalContext());
            }
            catch (Exception e)
            {
                Debug.LogError("Phonon Geometry not attached. " + e.Message);
            }
        }

        // Global Material Presets
        public PhononMaterialPreset materialPreset;
        public PhononMaterialValue materialValue;

        // Simulation Settings
        public SimulationSettingsPreset simulationPreset;
        public SimulationSettingsValue simulationValue;

        // Audio Engine
        public AudioEngine audioEngine;

        // Advaned Options
        public bool updateComponents = true;

        public bool showLoadTimeOptions = false;
        public bool showMassBakingOptions = false;
        public UnityEngine.Object currentlyBakingObject = null;

        // Structures to encapsulate Phonon C API objects.
        PhononManagerContainer phononContainer = new PhononManagerContainer();

        // Unity components.
        AudioListener audioListener = null;
        PhononListener phononListener = null;
        PhononStaticListener phononStaticListener = null;
        CustomSpeakerLayout customSpeakerLayout = null;
        CustomPhononSettings customPhononSettings = null;

        // A component may not exist in the scene, so can't check for null.
        // Need a flag to check if query for a component has already been performed and results cached.
        bool isSetPhononStaticListener = false;
        bool isSetCustomSpeakerLayout = false;
        bool isSetCustomPhononSettings = false;

        // Flag to allow Initialization only once.
        bool isInitialized = false;
    }
}