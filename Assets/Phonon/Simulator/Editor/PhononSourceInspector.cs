//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Phonon
{

    //
    // PhononSourceInspector
    // Custom inspector for PhononSource components.
    //

    [CustomEditor(typeof(PhononSource))]
    [CanEditMultipleObjects]
    public class PhononSourceInspector : Editor
    {
        //
        // Draws the inspector.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Direct Sound UX
            PhononGUI.SectionHeader("Direct Sound");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("directBinauralEnabled"));
            if (serializedObject.FindProperty("directBinauralEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hrtfInterpolation"), new GUIContent("HRTF Interpolation"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("directOcclusionOption"));
            if (serializedObject.FindProperty("directOcclusionOption").enumValueIndex == (int) OcclusionOption.Partial)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("partialOcclusionRadius"), new GUIContent("Source Radius (meters)"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("physicsBasedAttenuation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("directMixFraction"));

            // Indirect Sound UX
            PhononGUI.SectionHeader("Reflected Sound");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableReflections"));

            if (serializedObject.FindProperty("enableReflections").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceSimulationType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("indirectMixFraction"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("indirectBinauralEnabled"));

                EditorGUILayout.HelpBox("Go to Window > Phonon > Simulation to update the global simulation settings.", MessageType.Info);
                if (serializedObject.FindProperty("indirectBinauralEnabled").boolValue)
                    EditorGUILayout.HelpBox("The binaural setting is ignored if Phonon Listener component is attached with mixing enabled.", MessageType.Info);

                PhononSource phononEffect = serializedObject.targetObject as PhononSource;
                if (phononEffect.sourceSimulationType == SourceSimulationType.BakedStaticSource)
                {
                    BakedSourceGUI();
                    bakedStatsFoldout = EditorGUILayout.Foldout(bakedStatsFoldout, "Baked Static Source Statistics", true);
                    if (bakedStatsFoldout)
                        BakedSourceStatsGUI();
                }
            }

            // Save changes.
            serializedObject.ApplyModifiedProperties();
        }

        public void BakedSourceGUI()
        {
            PhononGUI.SectionHeader("Baked Static Source Settings");

            PhononSource bakedSource = serializedObject.targetObject as PhononSource;
            GUI.enabled = !bakedSource.phononBaker.IsBakeActive();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uniqueIdentifier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bakingRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useAllProbeBoxes"));

            if (!serializedObject.FindProperty("useAllProbeBoxes").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("probeBoxes"), true);

            bakedSource.uniqueIdentifier = bakedSource.uniqueIdentifier.Trim();
            if (bakedSource.uniqueIdentifier.Length == 0)
                EditorGUILayout.HelpBox("You must specify a unique identifier name.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Bake Effect"))
            {
                if (bakedSource.uniqueIdentifier.Length == 0)
                    Debug.LogError("You must specify a unique identifier name.");
                else
                {
                    Debug.Log("START: Baking effect for \"" + bakedSource.uniqueIdentifier + "\" with influence radius of " + bakedSource.bakingRadius + " meters.");
                    bakedSource.BeginBake();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            DisplayProgressBarAndCancel();

            if (bakedSource.phononBaker.GetBakeStatus() == BakeStatus.Complete)
            {
                bakedSource.EndBake();
                Repaint();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("COMPLETED: Baking effect for \"" + bakedSource.uniqueIdentifier + "\" with influence radius of " + bakedSource.bakingRadius + " meters.");
            }
        }

        public void BakedSourceStatsGUI()
        {
            PhononSource bakedSource = serializedObject.targetObject as PhononSource;
            GUI.enabled = !bakedSource.phononBaker.IsBakeActive();
            bakedSource.UpdateBakedDataStatistics();
            for (int i = 0; i < bakedSource.bakedProbeNames.Count; ++i)
                EditorGUILayout.LabelField(bakedSource.bakedProbeNames[i], (bakedSource.bakedProbeDataSizes[i] / 1000.0f).ToString("0.0") + " KB");
            EditorGUILayout.LabelField("Total Size", (bakedSource.bakedDataSize / 1000.0f).ToString("0.0") + " KB");
            GUI.enabled = true;
        }

        void DisplayProgressBarAndCancel()
        {
            PhononSource bakedSource = serializedObject.targetObject as PhononSource;
            bakedSource.phononBaker.DrawProgressBar();
            Repaint();
        }

        bool bakedStatsFoldout = false;
    }
}