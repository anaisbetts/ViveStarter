//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;
using System.Runtime.InteropServices;

namespace Phonon
{
    public class BinauralRenderer
    {
        public void Create(Environment environment, RenderingSettings renderingSettings, GlobalContext globalContext)
        {
            HRTFParams hrtfParams = new HRTFParams
            {
                type = HRTFDatabaseType.Default,
                hrtfData = IntPtr.Zero,
                numHrirSamples = 0,
                loadCallback = null,
                unloadCallback = null,
                lookupCallback = null
            };

            var error = PhononCore.iplCreateBinauralRenderer(globalContext, renderingSettings, hrtfParams, ref binauralRenderer);
            if (error != Error.None)
                throw new Exception("Unable to create binaural renderer [" + error.ToString() + "]");
        }

        public IntPtr GetBinauralRenderer()
        {
            return binauralRenderer;
        }

        public void Destroy()
        {
            if (binauralRenderer != IntPtr.Zero)
                PhononCore.iplDestroyBinauralRenderer(ref binauralRenderer);
        }

        IntPtr binauralRenderer = IntPtr.Zero;
    }
}