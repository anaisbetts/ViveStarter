using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Phonon
{
    public class IndirectMixer
    {
        public void Initialize(AudioFormat audioFormat, SimulationSettings simulationSettings)
        {
            outputFormat = audioFormat;

            ambisonicsFormat.channelLayoutType = ChannelLayoutType.Ambisonics;
            ambisonicsFormat.ambisonicsOrder = simulationSettings.ambisonicsOrder;
            ambisonicsFormat.numSpeakers = (ambisonicsFormat.ambisonicsOrder + 1) * (ambisonicsFormat.ambisonicsOrder + 1);
            ambisonicsFormat.ambisonicsOrdering = AmbisonicsOrdering.ACN;
            ambisonicsFormat.ambisonicsNormalization = AmbisonicsNormalization.N3D;
            ambisonicsFormat.channelOrder = ChannelOrder.Deinterleaved;
        }

        public void LazyInitialize(BinauralRenderer binauralRenderer, bool acceleratedMixing, bool indirectBinauralEnabled,
            RenderingSettings renderingSettings)
        {
#if !UNITY_ANDROID
            AudioFormat ambisonicsBinauralFormat = outputFormat;
            ambisonicsBinauralFormat.channelOrder = ChannelOrder.Deinterleaved;

            // Set up propagationPanningEffect
            if (acceleratedMixing && propagationPanningEffect == IntPtr.Zero 
                && binauralRenderer.GetBinauralRenderer() != IntPtr.Zero)
            {
                if (PhononCore.iplCreateAmbisonicsPanningEffect(binauralRenderer.GetBinauralRenderer(), ambisonicsFormat, 
                    ambisonicsBinauralFormat, ref propagationPanningEffect) != Error.None)
                {
                    Debug.Log("Unable to create Ambisonics panning effect. Please check the log file for details.");
                    return;
                }
            }

            if (outputFormat.channelLayout == ChannelLayout.Stereo && acceleratedMixing && indirectBinauralEnabled
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

            if (acceleratedMixing && wetData == null)
                wetData = new float[renderingSettings.frameSize * outputFormat.numSpeakers];

            if (acceleratedMixing && wetAmbisonicsDataMarshal == null)
            {
                wetAmbisonicsDataMarshal = new IntPtr[ambisonicsFormat.numSpeakers];
                for (int i = 0; i < ambisonicsFormat.numSpeakers; ++i)
                    wetAmbisonicsDataMarshal[i] = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(float)) * renderingSettings.frameSize);
            }

            if (acceleratedMixing && wetDataMarshal == null)
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
            PhononCore.iplDestroyAmbisonicsBinauralEffect(ref propagationBinauralEffect);
            PhononCore.iplDestroyAmbisonicsPanningEffect(ref propagationPanningEffect);

            propagationBinauralEffect = IntPtr.Zero;
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

        public void AudioFrameUpdate(float[] data, int channels, IntPtr environmentalRenderer, Vector3 listenerPosition, 
            Vector3 listenerAhead, Vector3 listenerUp, bool indirectBinauralEnabled)
        {

#if !UNITY_ANDROID
            AudioBuffer ambisonicsBuffer;
            ambisonicsBuffer.audioFormat = ambisonicsFormat;
            ambisonicsBuffer.numSamples = data.Length / channels;
            ambisonicsBuffer.deInterleavedBuffer = wetAmbisonicsDataMarshal;
            ambisonicsBuffer.interleavedBuffer = null;

            PhononCore.iplGetMixedEnvironmentalAudio(environmentalRenderer, listenerPosition, listenerAhead, listenerUp,
                ambisonicsBuffer);

            AudioBuffer spatializedBuffer;
            spatializedBuffer.audioFormat = outputFormat;
            spatializedBuffer.audioFormat.channelOrder = ChannelOrder.Deinterleaved;     // Set format to deinterleave.
            spatializedBuffer.numSamples = data.Length / channels;
            spatializedBuffer.deInterleavedBuffer = wetDataMarshal;
            spatializedBuffer.interleavedBuffer = null;

            if ((outputFormat.channelLayout == ChannelLayout.Stereo) && indirectBinauralEnabled)
                PhononCore.iplApplyAmbisonicsBinauralEffect(propagationBinauralEffect, ambisonicsBuffer, spatializedBuffer);
            else
                PhononCore.iplApplyAmbisonicsPanningEffect(propagationPanningEffect, ambisonicsBuffer, spatializedBuffer);

            AudioBuffer interleavedBuffer;
            interleavedBuffer.audioFormat = outputFormat;
            interleavedBuffer.numSamples = data.Length / channels;
            interleavedBuffer.deInterleavedBuffer = null;
            interleavedBuffer.interleavedBuffer = wetData;
            PhononCore.iplInterleaveAudioBuffer(spatializedBuffer, interleavedBuffer);

            for (int i = 0; i < data.Length; ++i)
                data[i] += wetData[i];
#endif
        }

        public void Flush()
        {
            PhononCore.iplFlushAmbisonicsPanningEffect(propagationPanningEffect);
            PhononCore.iplFlushAmbisonicsBinauralEffect(propagationBinauralEffect);
        }

        AudioFormat ambisonicsFormat;
        AudioFormat outputFormat;

        IntPtr propagationPanningEffect = IntPtr.Zero;
        IntPtr propagationBinauralEffect = IntPtr.Zero;

        float[] wetData = null;
        IntPtr[] wetDataMarshal = null;
        IntPtr[] wetAmbisonicsDataMarshal = null;
    }
}