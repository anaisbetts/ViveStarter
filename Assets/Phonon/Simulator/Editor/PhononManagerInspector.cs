//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using System;

namespace Phonon
{

    //
    // PhononManagerInspector
    // Custom inspector for a PhononManager component.
    //

    [CustomEditor(typeof(PhononManager))]
    public class PhononManagerInspector : Editor
    {
        //
        // Draws the inspector.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Audio Engine
            PhononGUI.SectionHeader("Audio Engine Integration");
            string[] engines = { "Unity Audio" };
            var audioEngineProperty = serializedObject.FindProperty("audioEngine");
            audioEngineProperty.enumValueIndex = EditorGUILayout.Popup("Audio Engine", audioEngineProperty.enumValueIndex, engines);

            // Scene Settings
            PhononManager phononManager = ((PhononManager)target);
            PhononGUI.SectionHeader("Global Material Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("materialPreset"));
            if (serializedObject.FindProperty("materialPreset").enumValueIndex < 11)
            {
                PhononMaterialValue actualValue = phononManager.materialValue;
                actualValue.CopyFrom(PhononMaterialPresetList.PresetValue(serializedObject.FindProperty("materialPreset").enumValueIndex));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("materialValue"));
            }

            PhononGUI.SectionHeader("Scene Export");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");

            bool exportOBJ = false;
            bool exportScene = false;

            if (GUILayout.Button("Export to OBJ"))
                exportOBJ = true;
            if (GUILayout.Button("Pre-Export Scene"))
                exportScene = true;

            if (exportOBJ || exportScene)
            {
                if (exportScene)
                    phononManager.ExportScene();
                if (exportOBJ)
                    phononManager.DumpScene();
            }
            EditorGUILayout.EndHorizontal();

            // Simulation Settings
            PhononGUI.SectionHeader("Simulation Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationPreset"));
            if (serializedObject.FindProperty("simulationPreset").enumValueIndex < 3)
            {
                SimulationSettingsValue actualValue = phononManager.simulationValue;
                actualValue.CopyFrom(SimulationSettingsPresetList.PresetValue(serializedObject.FindProperty("simulationPreset").enumValueIndex));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationValue"));
                if (Application.isEditor && EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SimulationSettingsValue actualValue = phononManager.simulationValue;
                    IntPtr environment = phononManager.PhononManagerContainer().Environment().GetEnvironment();
                    if (environment != IntPtr.Zero)
                        PhononCore.iplSetNumBounces(environment, actualValue.RealtimeBounces);
                }
            }

            // Fold Out for Advanced Settings
            PhononGUI.SectionHeader("Advanced Options");
            phononManager.showLoadTimeOptions = EditorGUILayout.Foldout(phononManager.showLoadTimeOptions, "Per Frame Query Optimization", true);
            if (phononManager.showLoadTimeOptions)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("updateComponents"));
            }

            phononManager.showMassBakingOptions = EditorGUILayout.Foldout(phononManager.showMassBakingOptions, "Consolidated Baking Options", true);
            if (phononManager.showMassBakingOptions)
            {
                bool noSettingMessage = false;
                noSettingMessage = ProbeGenerationGUI() || noSettingMessage;
                noSettingMessage = BakedSourcesGUI(phononManager) || noSettingMessage;
                noSettingMessage = BakedReverbGUI(phononManager) || noSettingMessage;
                noSettingMessage =  BakedStaticListenerNodeGUI(phononManager) || noSettingMessage;

                if (!noSettingMessage)
                    EditorGUILayout.LabelField("Scene does not contain any baking related components.");
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        public bool ProbeGenerationGUI()
        {
            ProbeBox[] probeBoxes = GameObject.FindObjectsOfType<ProbeBox>();
            if (probeBoxes.Length > 0)
                PhononGUI.SectionHeader("Probe Generation");
            else
                return false;

            foreach (ProbeBox probeBox in probeBoxes)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(probeBox.name);
                if (GUILayout.Button("Generate Probe", GUILayout.Width(200.0f)))
                    probeBox.GenerateProbes();
                EditorGUILayout.EndHorizontal();
            }

            return true;
        }

        public bool BakedSourcesGUI(PhononManager phononManager)
        {
            PhononSource[] bakedSources = GameObject.FindObjectsOfType<PhononSource>();

            bool showBakedSources = false;
            foreach (PhononSource bakedSource in bakedSources)
                if (bakedSource.enableReflections && bakedSource.uniqueIdentifier.Length != 0
                    && bakedSource.sourceSimulationType == SourceSimulationType.BakedStaticSource)
                {
                    showBakedSources = true;
                    break;
                }

            if (showBakedSources)
                PhononGUI.SectionHeader("Baked Sources");
            else
                return false;

            foreach (PhononSource bakedSource in bakedSources)
            {
                if (!bakedSource.enableReflections || bakedSource.uniqueIdentifier.Length == 0
                    || bakedSource.sourceSimulationType != SourceSimulationType.BakedStaticSource)
                    continue;

                GUI.enabled = !bakedSource.phononBaker.IsBakeActive();
                EditorGUILayout.BeginHorizontal();

                bakedSource.UpdateBakedDataStatistics();
                EditorGUILayout.LabelField(bakedSource.uniqueIdentifier, (bakedSource.bakedDataSize / 1000.0f).ToString("0.0") + " KB");
                if (GUILayout.Button("Bake Effect", GUILayout.Width(200.0f)))
                {
                    phononManager.currentlyBakingObject = bakedSource;
                    Debug.Log("START: Baking effect for \"" + bakedSource.uniqueIdentifier + "\" with influence radius of "
                        + bakedSource.bakingRadius + " meters.");
                    bakedSource.BeginBake();
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;

                DisplayProgressBarAndCancel(bakedSource, phononManager);

                if (bakedSource.phononBaker.GetBakeStatus() == BakeStatus.Complete)
                {
                    bakedSource.EndBake();
                    phononManager.currentlyBakingObject = null;
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    Debug.Log("COMPLETED: Baking effect for \"" + bakedSource.uniqueIdentifier + "\" with influence radius of "
                        + bakedSource.bakingRadius + " meters.");
                }
            }

            return true;
        }

        public bool BakedStaticListenerNodeGUI(PhononManager phononManager)
        {
            BakedStaticListenerNode[] bakedStaticNodes = GameObject.FindObjectsOfType<BakedStaticListenerNode>();

            if (bakedStaticNodes.Length > 0)
                PhononGUI.SectionHeader("Baked Static Listener Nodes");
            else
                return false;

            foreach (BakedStaticListenerNode bakedStaticNode in bakedStaticNodes)
            {
                if (bakedStaticNode.uniqueIdentifier.Length == 0)
                    continue;

                GUI.enabled = !bakedStaticNode.phononBaker.IsBakeActive();
                EditorGUILayout.BeginHorizontal();
                bakedStaticNode.UpdateBakedDataStatistics();
                EditorGUILayout.LabelField("__staticlistener__" + bakedStaticNode.uniqueIdentifier, (bakedStaticNode.bakedDataSize / 1000.0f).ToString("0.0") + " KB");
                if (GUILayout.Button("Bake Effect", GUILayout.Width(200.0f)))
                {
                    phononManager.currentlyBakingObject = bakedStaticNode;
                    Debug.Log("START: Baking effect for \"" + bakedStaticNode.uniqueIdentifier + "\".");
                    bakedStaticNode.BeginBake();
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;

                DisplayProgressBarAndCancel(bakedStaticNode, phononManager);

                if (bakedStaticNode.phononBaker.GetBakeStatus() == BakeStatus.Complete)
                {
                    bakedStaticNode.EndBake();
                    phononManager.currentlyBakingObject = null;
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    Debug.Log("COMPLETED: Baking effect for \"" + bakedStaticNode.uniqueIdentifier + "\".");
                }
            }

            return true;
        }

        public bool BakedReverbGUI(PhononManager phononManager)
        {
            PhononListener bakedReverb = GameObject.FindObjectOfType<PhononListener>();
            if (bakedReverb == null || !bakedReverb.enableReverb
                || bakedReverb.reverbSimulationType != ReverbSimulationType.BakedReverb)
                return false;

            PhononGUI.SectionHeader("Bake Reverb");

            GUI.enabled = !bakedReverb.phononBaker.IsBakeActive();
            EditorGUILayout.BeginHorizontal();
            bakedReverb.UpdateBakedDataStatistics();
            EditorGUILayout.LabelField("__reverb__", (bakedReverb.bakedDataSize / 1000.0f).ToString("0.0") + " KB");
            if (GUILayout.Button("Bake Reverb", GUILayout.Width(200.0f)))
            {
                Debug.Log("START: Baking reverb effect.");
                phononManager.currentlyBakingObject = bakedReverb;
                bakedReverb.BeginBake();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            DisplayProgressBarAndCancel(bakedReverb, phononManager);

            if (bakedReverb.phononBaker.GetBakeStatus() == BakeStatus.Complete)
            {
                bakedReverb.EndBake();
                phononManager.currentlyBakingObject = null;
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("COMPLETED: Baking reverb effect.");
            }

            return true;
        }

        void DisplayProgressBarAndCancel(PhononSource phononSource, PhononManager phononManager)
        {
            if (phononManager.currentlyBakingObject == null || phononManager.currentlyBakingObject != phononSource)
                return;

            phononSource.phononBaker.DrawProgressBar();
            Repaint();
        }

        void DisplayProgressBarAndCancel(PhononListener phononReverb, PhononManager phononManager)
        {
            if (phononManager.currentlyBakingObject == null)
                return;

            phononReverb.phononBaker.DrawProgressBar();
            Repaint();
        }

        void DisplayProgressBarAndCancel(BakedStaticListenerNode phononStaticNode, PhononManager phononManager)
        {
            if (phononManager.currentlyBakingObject == null || phononManager.currentlyBakingObject != phononStaticNode)
                return;

            phononStaticNode.phononBaker.DrawProgressBar();
            Repaint();
        }
    }
}