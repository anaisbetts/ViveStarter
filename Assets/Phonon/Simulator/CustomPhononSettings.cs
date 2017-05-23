//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;

namespace Phonon
{
    //
    // CustomPhononSettings
    // Custom Phonon Settings.
    //

    [AddComponentMenu("Phonon/Custom Phonon Settings")]
    public class CustomPhononSettings : MonoBehaviour
    {
        // Simulation settings.
        public SceneType rayTracerOption = SceneType.Phonon;

        //Renderer settings.
        public ConvolutionOption convolutionOption;

        //OpenCL settings.
        public bool useOpenCL = false;
        public ComputeDeviceType computeDeviceOption = ComputeDeviceType.Any;
        [Range(0, 8)]
        public int numComputeUnits = 4;
    }
}
