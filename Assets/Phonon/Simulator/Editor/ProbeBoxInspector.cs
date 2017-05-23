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
    // ProbeBoxInspector
    // Custom inspector for ProbeBox.
    //

    [CustomEditor(typeof(ProbeBox))]
    public class ProbeBoxInspector : Editor
    {
        //
        // Draws the inspector GUI.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            string[] placementStrategyString = { "Centroid", "Uniform Floor" };
            var placementStrategyProperty = serializedObject.FindProperty("placementStrategy");
            int enumValueIndex = (placementStrategyProperty.enumValueIndex > 0) ? 1 : 0;
            enumValueIndex = EditorGUILayout.Popup("Placement Strategy", enumValueIndex, placementStrategyString);
            placementStrategyProperty.enumValueIndex = (enumValueIndex > 0) ? 2 : 0;

            if (serializedObject.FindProperty("placementStrategy").intValue == (int) ProbePlacementStrategy.Octree)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxNumTriangles"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxOctreeDepth"));
            }
            else if (serializedObject.FindProperty("placementStrategy").intValue == (int)ProbePlacementStrategy.UniformFloor)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalSpacing"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("heightAboveFloor"));
            }

            ProbeBox probeBox = serializedObject.targetObject as ProbeBox;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Generate Probes"))
            {
                probeBox.GenerateProbes();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            EditorGUILayout.EndHorizontal();

            if (probeBox.probeSpherePoints != null && probeBox.probeSpherePoints.Length != 0)
            {
                PhononGUI.SectionHeader("Probe Box Statistics");
                EditorGUILayout.LabelField("Probe Points", (probeBox.probeSpherePoints.Length / 3).ToString());
                EditorGUILayout.LabelField("Probe Data Size", (probeBox.probeBoxData.Length / 1000.0f).ToString("0.0") + " KB");
            }

            for (int i = 0; i < probeBox.probeDataName.Count; ++i)
            {
                if (i == 0)
                    PhononGUI.SectionHeader("Detailed Statistics");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(probeBox.probeDataName[i], (probeBox.probeDataNameSizes[i] / 1000.0f).ToString("0.0") + " KB");
                if (GUILayout.Button("Clear"))
                    probeBox.DeleteBakedDataByName(probeBox.probeDataName[i]);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }
    }
}