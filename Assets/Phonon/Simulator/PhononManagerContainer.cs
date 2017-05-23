//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using System;

namespace Phonon
{
    public class PhononManagerContainer
    {
        // Initializes Phonon Manager Component, in particular various Phonon API handles contained within Phonon 
        // Manager Container. Initialize will be performed only once despite repeated calls to Initialize without 
        // calls to Destroy.
        public void Initialize(bool initializeRenderer, PhononManager phononManager)
        {
            if (refCounter++ != 0)
                return;

            bool useOpenCL;
            int numComputeUnits;
            ComputeDeviceType deviceType;
            deviceType = phononManager.ComputeDeviceSettings(out numComputeUnits, out useOpenCL);

            SimulationSettings simulationSettings = phononManager.SimulationSettings();
            GlobalContext globalContext = phononManager.GlobalContext();
            RenderingSettings renderingSettings = phononManager.RenderingSettings();

            try
            {
                computeDevice.Create(useOpenCL, deviceType, numComputeUnits);
            }
            catch (Exception e)
            {
                throw e;
            }

            probeManager.Create();

            if (scene.Create(computeDevice, simulationSettings, globalContext) == Error.None)
            {
                try
                {
                    environment.Create(computeDevice, simulationSettings, scene, probeManager, globalContext);
                }
                catch (Exception e)
                {
                    throw e;
                }

                if (initializeRenderer)
                    environmentRenderer.Create(environment, renderingSettings, simulationSettings, globalContext);
            }

            if (initializeRenderer)
                binauralRenderer.Create(environment, renderingSettings, globalContext);
        }

        // Destroys Phonon Manager.
        public void Destroy()
        {
            --refCounter;
            if (refCounter != 0)
                return;

            environment.Destroy();
            scene.Destroy();
            computeDevice.Destroy();
            probeManager.Destroy();
            binauralRenderer.Destroy();
            environmentRenderer.Destroy();
        }

        // Returns Scene. Phonon Manager Initialization sets up the Scene.
        public Scene Scene()
        {
            return scene;
        }

        // Returns Probe Manager. Phonon Manager Initialization sets up the Probe Manager.
        public ProbeManager ProbeManager()
        {
            return probeManager;
        }

        // Returns Environment. Phonon Manager Initialization sets up the Environment.
        public Environment Environment()
        {
            return environment;
        }

        // Returns Environmental Renderer. Phonon Manager Initialization sets up the Environmental Renderer.
        public EnvironmentalRenderer EnvironmentalRenderer()
        {
            return environmentRenderer;
        }

        // Returns Binaural Renderer. Phonon Manager Initialization sets up the Binaural Renderer.
        public BinauralRenderer BinauralRenderer()
        {
            return binauralRenderer;
        }

        // Structures to encapsulate Phonon C API objects.
        ComputeDevice computeDevice = new ComputeDevice();
        Scene scene = new Scene();
        Environment environment = new Environment();
        EnvironmentalRenderer environmentRenderer = new EnvironmentalRenderer();
        BinauralRenderer binauralRenderer = new BinauralRenderer();
        ProbeManager probeManager = new ProbeManager();

        // Counter to keep track of references.
        int refCounter = 0;
    }
}