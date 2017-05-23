using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Phonon
{
    public class IndirectSimulator
    {
        public void Initialize(AudioFormat audioFormat, SimulationSettings simulationSettings)
        {
            inputFormat = audioFormat;
            outputFormat = audioFormat;

            ambisonicsFormat.channelLayoutType = ChannelLayoutType.Ambisonics;
            ambisonicsFormat.ambisonicsOrder = simulationSettings.ambisonicsOrder;
            ambisonicsFormat.numSpeakers = (ambisonicsFormat.ambisonicsOrder + 1) * (ambisonicsFormat.ambisonicsOrder + 1);
            ambisonicsFormat.ambisonicsOrdering = AmbisonicsOrdering.ACN;
            ambisonicsFormat.ambisonicsNormalization = AmbisonicsNormalization.N3D;
            ambisonicsFormat.channelOrder = ChannelOrder.Deinterleaved;
        }

        public void LazyInitialize(BinauralRenderer binauralRenderer, bool reflectionEnabled, bool indirectBinauralEnabled,
            RenderingSettings renderingSettings, bool sourceUpdate, SourceSimulationType sourceSimulationType, 
            string uniqueIdentifier, PhononStaticListener phononStaticListener, ReverbSimulationType reverbSimualtionType,
            EnvironmentalRenderer environmentalRenderer)
        {
#if !UNITY_ANDROID
            AudioFormat ambisonicsBinauralFormat = outputFormat;
            ambisonicsBinauralFormat.channelOrder = ChannelOrder.Deinterleaved;

            // Set up propagationPanningEffect
            if (reflectionEnabled && propagationPanningEffect == IntPtr.Zero 
                && binauralRenderer.GetBinauralRenderer() != IntPtr.Zero)
            {
                if (PhononCore.iplCreateAmbisonicsPanningEffect(binauralRenderer.GetBinauralRenderer(), ambisonicsFormat, 
                    ambisonicsBinauralFormat, ref propagationPanningEffect) != Error.None)
                {
                    Debug.Log("Unable to create Ambisonics panning effect. Please check the log file for details.");
                    return;
                }
            }

            // Set up propagationBinauralEffect
            if (outputFormat.channelLayout == ChannelLayout.Stereo && reflectionEnabled && indirectBinauralEnabled
                && propagationBinauralEffect == IntPtr.Zero && binauralRenderer.GetBinauralRenderer() != IntPtr.Zero)
            {
                // Create ambisonics based binaural effect for indirect sound if the output format is stereo.
                if (PhononCore.iplCreateAmbisonicsBinauralEffect(binauralRenderer.GetBinauralRenderer(), ambisonicsFormat, 
                    ambisonicsBinauralFormat, ref propagationBinauralEffect) != Error.None)
                {
                    Debug.Log("Unable to create propagation binaural effect. Please check the log file for details.");
                    return;
                }
            }

            // Set up propagationAmbisonicsEffect
            if (reflectionEnabled && propagationAmbisonicsEffect == IntPtr.Zero 
                && environmentalRenderer.GetEnvironmentalRenderer() != IntPtr.Zero)
            {
                string effectName = "";

                if (sourceUpdate && sourceSimulationType == SourceSimulationType.BakedStaticSource)
                    effectName = uniqueIdentifier;
                else if (sourceUpdate && sourceSimulationType == SourceSimulationType.BakedStaticListener)
                {
                    if (phononStaticListener == null)
                        Debug.LogError("No Phonon Static Listener component found.");
                    else if (phononStaticListener.currentStaticListenerNode == null)
                        Debug.LogError("Current static listener node is not specified in Phonon Static Listener.");
                    else
                        effectName = phononStaticListener.currentStaticListenerNode.GetUniqueIdentifier();
                }
                else if (!sourceUpdate && reverbSimualtionType == ReverbSimulationType.BakedReverb)
                    effectName = "__reverb__";

                SimulationType simulationMode;
                if (sourceUpdate)
                    simulationMode = (sourceSimulationType == SourceSimulationType.Realtime) ? 
                        SimulationType.Realtime : SimulationType.Baked;
                else
                    simulationMode = (reverbSimualtionType == ReverbSimulationType.RealtimeReverb) ? 
                        SimulationType.Realtime : SimulationType.Baked;

                if (PhononCore.iplCreateConvolutionEffect(environmentalRenderer.GetEnvironmentalRenderer(),
                    Common.ConvertString(effectName),  simulationMode, inputFormat, ambisonicsFormat,
                    ref propagationAmbisonicsEffect) != Error.None)
                {
                    Debug.LogError("Unable to create propagation effect for object");
                }
            }

            if (reflectionEnabled && wetData == null)
                wetData = new float[renderingSettings.frameSize * outputFormat.numSpeakers];

            if (reflectionEnabled && wetAmbisonicsDataMarshal == null)
            {
                wetAmbisonicsDataMarshal = new IntPtr[ambisonicsFormat.numSpeakers];
                for (int i = 0; i < ambisonicsFormat.numSpeakers; ++i)
                    wetAmbisonicsDataMarshal[i] = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(float)) * renderingSettings.frameSize);
            }

            if (reflectionEnabled && wetDataMarshal == null)
            {
                wetDataMarshal = new IntPtr[outputFormat.numSpeakers];
                for (int i = 0; i < outputFormat.numSpeakers; ++i)
                    wetDataMarshal[i] = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(float)) * renderingSettings.frameSize);
            }
#endif
        }

        public void Destroy()
        {
#if !UNITY_ANDROID
            PhononCore.iplDestroyConvolutionEffect(ref propagationAmbisonicsEffect);
            propagationAmbisonicsEffect = IntPtr.Zero;

            PhononCore.iplDestroyAmbisonicsBinauralEffect(ref propagationBinauralEffect);
            propagationBinauralEffect = IntPtr.Zero;

            PhononCore.iplDestroyAmbisonicsPanningEffect(ref propagationPanningEffect);
            propagationPanningEffect = IntPtr.Zero;

            wetData = null;

            if (wetDataMarshal != null)
                for (int i = 0; i < outputFormat.numSpeakers; ++i)
                    Marshal.FreeCoTaskMem(wetDataMarshal[i]);
            wetDataMarshal = null;

            if (wetAmbisonicsDataMarshal != null)
                for (int i = 0; i < ambisonicsFormat.numSpeakers; ++i)
                    Marshal.FreeCoTaskMem(wetAmbisonicsDataMarshal[i]);
            wetAmbisonicsDataMarshal = null;
#endif
        }

        public float[] AudioFrameUpdate(float[] data, int channels, Vector3 sourcePosition, Vector3 listenerPosition, 
            Vector3 listenerAhead, Vector3 listenerUp, bool enableReflections, float indirectMixFraction, 
            bool indirectBinauralEnabled, PhononListener phononListener)
        {
#if !UNITY_ANDROID
            AudioBuffer inputBuffer;
            inputBuffer.audioFormat = inputFormat;
            inputBuffer.numSamples = data.Length / channels;
            inputBuffer.deInterleavedBuffer = null;
            inputBuffer.interleavedBuffer = data;

            AudioBuffer outputBuffer;
            outputBuffer.audioFormat = outputFormat;
            outputBuffer.numSamples = data.Length / channels;
            outputBuffer.deInterleavedBuffer = null;
            outputBuffer.interleavedBuffer = data;

            // Input data is sent (where it is copied) for indirect propagation effect processing.
            // This data must be sent before applying any other effect to the input audio.
            if (enableReflections && (wetData != null) && (wetDataMarshal != null) && (wetAmbisonicsDataMarshal != null) && (propagationAmbisonicsEffect != IntPtr.Zero))
            {
                for (int i = 0; i < data.Length; ++i)
                    wetData[i] = data[i] * indirectMixFraction;

                AudioBuffer propagationInputBuffer;
                propagationInputBuffer.audioFormat = inputFormat;
                propagationInputBuffer.numSamples = wetData.Length / channels;
                propagationInputBuffer.deInterleavedBuffer = null;
                propagationInputBuffer.interleavedBuffer = wetData;

                PhononCore.iplSetDryAudioForConvolutionEffect(propagationAmbisonicsEffect, sourcePosition, propagationInputBuffer);

                if (fourierMixingEnabled)
                {
                    phononListener.processMixedAudio = true;
                    return null;
                }

                AudioBuffer wetAmbisonicsBuffer;
                wetAmbisonicsBuffer.audioFormat = ambisonicsFormat;
                wetAmbisonicsBuffer.numSamples = data.Length / channels;
                wetAmbisonicsBuffer.deInterleavedBuffer = wetAmbisonicsDataMarshal;
                wetAmbisonicsBuffer.interleavedBuffer = null;
                PhononCore.iplGetWetAudioForConvolutionEffect(propagationAmbisonicsEffect, listenerPosition, listenerAhead, listenerUp, wetAmbisonicsBuffer);

                AudioBuffer wetBufferMarshal;
                wetBufferMarshal.audioFormat = outputFormat;
                wetBufferMarshal.audioFormat.channelOrder = ChannelOrder.Deinterleaved;     // Set format to deinterleave.
                wetBufferMarshal.numSamples = data.Length / channels;
                wetBufferMarshal.deInterleavedBuffer = wetDataMarshal;
                wetBufferMarshal.interleavedBuffer = null;

                if ((outputFormat.channelLayout == ChannelLayout.Stereo) && indirectBinauralEnabled)
                    PhononCore.iplApplyAmbisonicsBinauralEffect(propagationBinauralEffect, wetAmbisonicsBuffer, wetBufferMarshal);
                else
                    PhononCore.iplApplyAmbisonicsPanningEffect(propagationPanningEffect, wetAmbisonicsBuffer, wetBufferMarshal);

                AudioBuffer wetBuffer;
                wetBuffer.audioFormat = outputFormat;
                wetBuffer.numSamples = data.Length / channels;
                wetBuffer.deInterleavedBuffer = null;
                wetBuffer.interleavedBuffer = wetData;
                PhononCore.iplInterleaveAudioBuffer(wetBufferMarshal, wetBuffer);

                return wetData;
            }
#endif

            return null;
        }

        public void FrameUpdate(bool sourceUpdate, SourceSimulationType sourceSimulationType, 
            ReverbSimulationType reverbSimulationType, PhononStaticListener phononStaticListener, PhononListener phononListener)
        {
            if (sourceUpdate && sourceSimulationType == SourceSimulationType.BakedStaticListener && phononStaticListener != null 
                && phononStaticListener.currentStaticListenerNode != null)
                UpdateEffectName(phononStaticListener.currentStaticListenerNode.GetUniqueIdentifier());

            if (phononListener && phononListener.acceleratedMixing)
                fourierMixingEnabled = true;
            else
                fourierMixingEnabled = false;
        }

        public void Flush()
        {
            PhononCore.iplFlushAmbisonicsPanningEffect(propagationPanningEffect);
            PhononCore.iplFlushAmbisonicsBinauralEffect(propagationBinauralEffect);
            PhononCore.iplFlushConvolutionEffect(propagationAmbisonicsEffect);
        }

        //
        // Helper function to change the name of the BakedSource or BakedStaticListener
        // used by the effect.
        //
        public void UpdateEffectName(string effectName)
        {
            if (propagationAmbisonicsEffect != IntPtr.Zero)
                PhononCore.iplSetConvolutionEffectName(propagationAmbisonicsEffect, Common.ConvertString(effectName));
        }

        AudioFormat inputFormat;
        AudioFormat outputFormat;
        AudioFormat ambisonicsFormat;

        float[] wetData = null;
        IntPtr[] wetDataMarshal = null;
        IntPtr[] wetAmbisonicsDataMarshal = null;
        bool fourierMixingEnabled;

        // Phonon API related variables.
        IntPtr propagationPanningEffect = IntPtr.Zero;
        IntPtr propagationBinauralEffect = IntPtr.Zero;
        IntPtr propagationAmbisonicsEffect = IntPtr.Zero;
    }
}