//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;

namespace Phonon
{
    public class ComputeDevice
    {
        public IntPtr GetDevice()
        {
            return device;
        }

        public Error Create(bool useOpenCL, ComputeDeviceType deviceType, int numComputeUnits)
        {
            var error = Error.None;

            if (useOpenCL)
            {
                error = PhononCore.iplCreateComputeDevice(deviceType, numComputeUnits, ref device);
                if (error != Error.None)
                {
                    throw new Exception("Unable to create OpenCL compute device (" + deviceType.ToString() + ", " +
                        numComputeUnits.ToString() + " CUs): [" + error.ToString() + "]");
                }
            }

            return error;
        }

        public void Destroy()
        {
            if (device != IntPtr.Zero)
                PhononCore.iplDestroyComputeDevice(ref device);
        }

        IntPtr device = IntPtr.Zero;
    }
}