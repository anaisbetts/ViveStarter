//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

namespace Phonon
{
    //
    // SourceSimulationType
    // Various simulation options for a PhononSource.
    //
    public enum SourceSimulationType
    {
        Realtime,
        BakedStaticSource,
        BakedStaticListener
    }

    //
    // PhononSource
    // Enables physics-based modeling for any object with AudioSource component.
    //
    [AddComponentMenu("Phonon/Phonon Source")]
    public class PhononSource : MonoBehaviour
    {
        private void Awake()
        {
            Initialize();
            LazyInitialize();
        }

        private void OnEnable()
        {
            StartCoroutine(EndOfFrameUpdate());
        }

        private void OnDisable()
        {
            directSimulator.Flush();
            indirectSimulator.Flush();
        }

        private void OnDestroy()
        {
            Destroy();
        }

        private void Initialize()
        {
            initialized = false;
            destroying = false;
            errorLogged = false;

            phononManager = FindObjectOfType<PhononManager>();
            if (phononManager == null)
            {
                Debug.LogError("Phonon Manager Settings object not found in the scene! Click Window > Phonon");
                return;
            }

            bool initializeRenderer = true;
            phononManager.Initialize(initializeRenderer);
            phononContainer = phononManager.PhononManagerContainer();
            phononContainer.Initialize(initializeRenderer, phononManager);

            directSimulator.Initialize(phononManager.AudioFormat());
            indirectSimulator.Initialize(phononManager.AudioFormat(), phononManager.SimulationSettings());
        }

        private void LazyInitialize()
        {
            if (phononManager != null && phononContainer != null)
            {
                directSimulator.LazyInitialize(phononContainer.BinauralRenderer(), directBinauralEnabled);

                indirectSimulator.LazyInitialize(phononContainer.BinauralRenderer(), enableReflections,
                    indirectBinauralEnabled, phononManager.RenderingSettings(), true, sourceSimulationType,
                    uniqueIdentifier, phononManager.PhononStaticListener(), ReverbSimulationType.RealtimeReverb,
                    phononContainer.EnvironmentalRenderer());
            }
        }

        private void Destroy()
        {
            mutex.WaitOne();
            destroying = true;

            directSimulator.Destroy();
            indirectSimulator.Destroy();

            if (phononContainer != null)
            {
                phononContainer.Destroy();
                phononContainer = null;
            }

            mutex.ReleaseMutex();
        }

        //
        // Courutine to update source and listener position and orientation at frame end.
        // Done this way to ensure correct update in VR setup.
        //
        private IEnumerator EndOfFrameUpdate()
        {
            while (true)
            {
                LazyInitialize();

                if (!errorLogged && phononManager != null && phononContainer != null 
                    && phononContainer.Scene().GetScene() == IntPtr.Zero
                    && ((directOcclusionOption != OcclusionOption.None) || enableReflections))
                {
                    Debug.LogError("Scene not found. Make sure to pre-export the scene.");
                    errorLogged = true;
                }

                if (phononManager != null && !errorLogged) //Output silence in case if scene is missing and required.
                {
                    UpdateRelativeDirection();
                    directSimulator.FrameUpdate(phononContainer.EnvironmentalRenderer().GetEnvironmentalRenderer(),
                        sourcePosition, listenerPosition, listenerAhead, listenerUp, partialOcclusionRadius, directOcclusionOption);
                    indirectSimulator.FrameUpdate(true, sourceSimulationType, ReverbSimulationType.RealtimeReverb,
                        phononManager.PhononStaticListener(), phononManager.PhononListener());

                    initialized = true;
                }

                yield return new WaitForEndOfFrame();   // Must yield after updating the relative direction.
            }
        }

        //
        // Updates the direction of the source relative to the listener.
        // Wait until the end of the frame to update the position to get latest information.
        //
        private void UpdateRelativeDirection()
        {
            AudioListener listener = phononManager.AudioListener();
            if (listener == null) return;

            sourcePosition = Common.ConvertVector(transform.position);
            listenerPosition = Common.ConvertVector(listener.transform.position);
            listenerAhead = Common.ConvertVector(listener.transform.forward);
            listenerUp = Common.ConvertVector(listener.transform.up);
        }

        //
        // Applies propagtion effects to dry audio.
        //
        void OnAudioFilterRead(float[] data, int channels)
        {
            mutex.WaitOne();

            if (data == null)
            {
                mutex.ReleaseMutex();
                return;
            }

            if (!initialized || destroying)
            {
                mutex.ReleaseMutex();
                Array.Clear(data, 0, data.Length);
                return;
            }

            //data is copied, must be used before directSimulator which modifies the data.
            float[] wetData = indirectSimulator.AudioFrameUpdate(data, channels, sourcePosition, listenerPosition, 
                                listenerAhead, listenerUp, enableReflections, indirectMixFraction, 
                                indirectBinauralEnabled, phononManager.PhononListener());

            directSimulator.AudioFrameUpdate(data, channels, physicsBasedAttenuation, directMixFraction, 
                directBinauralEnabled, hrtfInterpolation);

            if (wetData != null && wetData.Length != 0)
                for (int i = 0; i < data.Length; ++i)
                    data[i] += wetData[i];

            mutex.ReleaseMutex();
        }

        public void BeginBake()
        {
            Sphere bakeSphere;
            Vector3 sphereCenter = Common.ConvertVector(gameObject.transform.position);
            bakeSphere.centerx = sphereCenter.x;
            bakeSphere.centery = sphereCenter.y;
            bakeSphere.centerz = sphereCenter.z;
            bakeSphere.radius = bakingRadius;

            if (useAllProbeBoxes)
                phononBaker.BeginBake(FindObjectsOfType<ProbeBox>() as ProbeBox[], BakingMode.StaticSource, 
                    uniqueIdentifier, bakeSphere);
            else
                phononBaker.BeginBake(probeBoxes, BakingMode.StaticSource, uniqueIdentifier, bakeSphere);
        }

        public void EndBake()
        {
            phononBaker.EndBake();
        }

        void OnDrawGizmosSelected()
        {
            if (sourceSimulationType == SourceSimulationType.BakedStaticSource)
            {
                Color oldColor = Gizmos.color;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(gameObject.transform.position, bakingRadius);

                Gizmos.color = Color.magenta;
                ProbeBox[] drawProbeBoxes = probeBoxes;
                if (useAllProbeBoxes)
                    drawProbeBoxes = FindObjectsOfType<ProbeBox>() as ProbeBox[];

                if (drawProbeBoxes != null)
                    foreach (ProbeBox probeBox in drawProbeBoxes)
                        if (probeBox != null)
                            Gizmos.DrawWireCube(probeBox.transform.position, probeBox.transform.localScale);

                Gizmos.color = oldColor;
            }
        }

        public void UpdateBakedDataStatistics()
        {
            ProbeBox[] statProbeBoxes = probeBoxes;
            if (useAllProbeBoxes)
                statProbeBoxes = FindObjectsOfType<ProbeBox>() as ProbeBox[];

            if (statProbeBoxes == null)
                return;

            int dataSize = 0;
            List<string> probeNames = new List<string>();
            List<int> probeDataSizes = new List<int>();
            foreach (ProbeBox probeBox in statProbeBoxes)
            {
                if (probeBox == null || uniqueIdentifier.Length == 0)
                    continue;

                int probeDataSize = 0;
                probeNames.Add(probeBox.name);

                for (int i = 0; i < probeBox.probeDataName.Count; ++i)
                {
                    if (uniqueIdentifier == probeBox.probeDataName[i])
                    {
                        probeDataSize = probeBox.probeDataNameSizes[i];
                        dataSize += probeDataSize;
                    }
                }

                probeDataSizes.Add(probeDataSize);
            }

            bakedDataSize = dataSize;
            bakedProbeNames = probeNames;
            bakedProbeDataSizes = probeDataSizes;
        }

        // Public fields - direct sound.
        public bool directBinauralEnabled = true;
        public HRTFInterpolation hrtfInterpolation;
        public OcclusionOption directOcclusionOption;
        [Range(.1f, 32f)]
        public float partialOcclusionRadius = 1.0f;
        public bool physicsBasedAttenuation = true;
        [Range(.0f, 1.0f)]
        public float directMixFraction = 1.0f;

        // Public fields - indirect sound.
        public bool enableReflections = false;
        public SourceSimulationType sourceSimulationType;
        [Range(.0f, 10.0f)]
        public float indirectMixFraction = 1.0f;
        public bool indirectBinauralEnabled = false;

        // Public fields - indirect baking.
        public string uniqueIdentifier = "";
        [Range(1f, 1024f)]
        public float bakingRadius = 16f;
        public bool useAllProbeBoxes = false;
        public ProbeBox[] probeBoxes = null;
        public PhononBaker phononBaker = new PhononBaker();

        // Public stored fields - baking.
        public List<string> bakedProbeNames = new List<string>();
        public List<int> bakedProbeDataSizes = new List<int>();
        public int bakedDataSize = 0;

        // Private fields.
        PhononManager phononManager = null;
        PhononManagerContainer phononContainer = null;

        AudioFormat inputFormat;
        AudioFormat outputFormat;
        AudioFormat ambisonicsFormat;

        Vector3 sourcePosition;
        Vector3 listenerPosition;
        Vector3 listenerAhead;
        Vector3 listenerUp;

        Mutex mutex = new Mutex();

        bool initialized = false;
        bool destroying = false;
        bool errorLogged = false;

        DirectSimulator directSimulator = new DirectSimulator();
        IndirectSimulator indirectSimulator = new IndirectSimulator();
    }
}
