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
    public enum ReverbSimulationType
    {
        RealtimeReverb,
        BakedReverb,
    }

    //
    // PhononListener
    // Represents a Phonon Listener. Performs optimized mixing in fourier
    // domain or apply reverb.
    //

    [AddComponentMenu("Phonon/Phonon Listener")]
    public class PhononListener : MonoBehaviour
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
            indirectMixer.Flush();
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

            indirectSimulator.Initialize(phononManager.AudioFormat(), phononManager.SimulationSettings());
            indirectMixer.Initialize(phononManager.AudioFormat(), phononManager.SimulationSettings());
        }

        private void LazyInitialize()
        {
            if (phononManager != null && phononContainer != null)
            {
                indirectSimulator.LazyInitialize(phononContainer.BinauralRenderer(), enableReverb && !acceleratedMixing,
                    indirectBinauralEnabled, phononManager.RenderingSettings(), false, SourceSimulationType.Realtime,
                    "__reverb__", phononManager.PhononStaticListener(), reverbSimulationType,
                    phononContainer.EnvironmentalRenderer());

                indirectMixer.LazyInitialize(phononContainer.BinauralRenderer(), acceleratedMixing, indirectBinauralEnabled,
                    phononManager.RenderingSettings());
            }
        }

        private void Destroy()
        {
            mutex.WaitOne();
            destroying = true;

            indirectMixer.Destroy();
            indirectSimulator.Destroy();

            if (phononContainer != null)
            {
                phononContainer.Destroy();
                phononContainer = null;
            }

            mutex.ReleaseMutex();
        }


        //
        // Courutine to update listener position and orientation at frame end.
        // Done this way to ensure correct update in VR setup.
        //
        private IEnumerator EndOfFrameUpdate()
        {
            while (true)
            {
                LazyInitialize();

                if (!errorLogged && phononManager != null && phononContainer !=null 
                    && phononContainer.Scene().GetScene() == IntPtr.Zero && enableReverb)
                {
                    Debug.LogError("Scene not found. Make sure to pre-export the scene.");
                    errorLogged = true;
                }

                if (!initialized && phononManager != null && phononContainer != null
                    && phononContainer.EnvironmentalRenderer().GetEnvironmentalRenderer() != IntPtr.Zero)
                {
                    initialized = true;
                }

                if (phononManager != null)
                {
                    listenerPosition = Common.ConvertVector(transform.position);
                    listenerAhead = Common.ConvertVector(transform.forward);
                    listenerUp = Common.ConvertVector(transform.up);
                    indirectSimulator.FrameUpdate(false, SourceSimulationType.Realtime, reverbSimulationType,
                        phononManager.PhononStaticListener(), phononManager.PhononListener());
                }

                yield return new WaitForEndOfFrame();
            }
        }

        //
        // Applies the Phonon effect to audio.
        //
        void OnAudioFilterRead(float[] data, int channels)
        {
            mutex.WaitOne();

            if (data == null)
            {
                mutex.ReleaseMutex();
                return;
            }

            if (!initialized || destroying || (acceleratedMixing && !processMixedAudio))
            {
                mutex.ReleaseMutex();
                Array.Clear(data, 0, data.Length);
                return;
            }

            if (acceleratedMixing)
                indirectMixer.AudioFrameUpdate(data, channels, phononContainer.EnvironmentalRenderer().GetEnvironmentalRenderer(),
                    listenerPosition, listenerAhead, listenerUp, indirectBinauralEnabled);
            else if (enableReverb)
            {
                float[] wetData = indirectSimulator.AudioFrameUpdate(data, channels, listenerPosition, listenerPosition, 
                    listenerAhead, listenerUp, enableReverb, reverbMixFraction, indirectBinauralEnabled, phononManager.PhononListener());
                if (wetData != null && wetData.Length != 0)
                    for (int i = 0; i < data.Length; ++i)
                        data[i] = data[i] * dryMixFraction + wetData[i];
            }

            mutex.ReleaseMutex();
        }

        void OnDrawGizmosSelected()
        {
            Color oldColor = Gizmos.color;

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

        public void BeginBake()
        {
            if (useAllProbeBoxes)
                phononBaker.BeginBake(FindObjectsOfType<ProbeBox>() as ProbeBox[], BakingMode.Reverb, "__reverb__");
            else
                phononBaker.BeginBake(probeBoxes, BakingMode.Reverb, "__reverb__");
        }

        public void EndBake()
        {
            phononBaker.EndBake();
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
                if (probeBox == null)
                    continue;

                int probeDataSize = 0;
                probeNames.Add(probeBox.name);

                for (int i = 0; i < probeBox.probeDataName.Count; ++i)
                {
                    if ("__reverb__" == probeBox.probeDataName[i])
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

        // Public members.
        public bool processMixedAudio;
        public bool acceleratedMixing = false;

        public bool enableReverb = false;
        public ReverbSimulationType reverbSimulationType;
        [Range(.0f, 1.0f)]
        public float dryMixFraction = 1.0f;
        [Range(.0f, 10.0f)]
        public float reverbMixFraction = 1.0f;

        public bool indirectBinauralEnabled = false;

        public bool useAllProbeBoxes = false;
        public ProbeBox[] probeBoxes = null;

        // Public stored fields - baking.
        public List<string> bakedProbeNames = new List<string>();
        public List<int> bakedProbeDataSizes = new List<int>();
        public int bakedDataSize = 0;

        // Private members.
        PhononManager phononManager = null;
        PhononManagerContainer phononContainer = null;

        IndirectMixer indirectMixer = new IndirectMixer();
        IndirectSimulator indirectSimulator = new IndirectSimulator();
        public PhononBaker phononBaker = new PhononBaker();

        Vector3 listenerPosition;
        Vector3 listenerAhead;
        Vector3 listenerUp;

        Mutex mutex = new Mutex();
        bool initialized = false;
        bool destroying = false;
        bool errorLogged = false;
    }
}