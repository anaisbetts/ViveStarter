//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;

namespace Phonon
{
    public class EnvironmentalRenderer
    {
        public void Create(Environment environment, RenderingSettings renderingSettings, SimulationSettings simulationSettings, GlobalContext globalContext)
        {
            ambisonicsFormat.channelLayoutType = ChannelLayoutType.Ambisonics;
            ambisonicsFormat.ambisonicsOrder = simulationSettings.ambisonicsOrder;
            ambisonicsFormat.numSpeakers = (ambisonicsFormat.ambisonicsOrder + 1) * (ambisonicsFormat.ambisonicsOrder + 1);
            ambisonicsFormat.ambisonicsOrdering = AmbisonicsOrdering.ACN;
            ambisonicsFormat.ambisonicsNormalization = AmbisonicsNormalization.N3D;
            ambisonicsFormat.channelOrder = ChannelOrder.Deinterleaved;

            var error = PhononCore.iplCreateEnvironmentalRenderer(globalContext, environment.GetEnvironment(),
                renderingSettings, ambisonicsFormat, ref environmentalRenderer);
            if (error != Error.None)
            {
                throw new Exception("Unable to create environment renderer [" + error.ToString() + "]");
            }
        }

        public IntPtr GetEnvironmentalRenderer()
        {
            return environmentalRenderer;
        }

        public void Destroy()
        {
            PhononCore.iplDestroyEnvironmentalRenderer(ref environmentalRenderer);
        }

        AudioFormat ambisonicsFormat;
        IntPtr environmentalRenderer = IntPtr.Zero;
    }
}