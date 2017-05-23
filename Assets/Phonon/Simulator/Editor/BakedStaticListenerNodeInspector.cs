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
    // BakedStaticListenerInspector
    // Custom inspector for BakedStaticListener.
    //

    [CustomEditor(typeof(BakedStaticListenerNode))]
    public class BakedStaticListenerNodeInspector : Editor
    {
        //
        // Draws the inspector GUI.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PhononGUI.SectionHeader("Baked Static Listener Settings");

            BakedStaticListenerNode bakedStaticListener = serializedObject.targetObject as BakedStaticListenerNode;
            GUI.enabled = !bakedStaticListener.phononBaker.IsBakeActive();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uniqueIdentifier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bakingRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useAllProbeBoxes"));

            bakedStaticListener.uniqueIdentifier = bakedStaticListener.uniqueIdentifier.Trim();
            if (bakedStaticListener.uniqueIdentifier.Length == 0)
                EditorGUILayout.HelpBox("You must specify a unique identifier name.", MessageType.Warning);

            if (!serializedObject.FindProperty("useAllProbeBoxes").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("probeBoxes"), true);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Bake Effect"))
            {
                if (bakedStaticListener.uniqueIdentifier.Length == 0)
                    Debug.LogError("You must specify a unique identifier name.");
                else
                {
                    Debug.Log("START: Baking effect for \"" + bakedStaticListener.uniqueIdentifier + "\".");
                    bakedStaticListener.BeginBake();
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            DisplayProgressBarAndCancel();

            if (bakedStaticListener.phononBaker.GetBakeStatus() == BakeStatus.Complete)
            {
                bakedStaticListener.EndBake();
                Repaint();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("COMPLETED: Baking effect for \"" + bakedStaticListener.uniqueIdentifier + "\".");
            }

            bakedStatsFoldout = EditorGUILayout.Foldout(bakedStatsFoldout, "Baked Static Listener Node Statistics", true);
            if (bakedStatsFoldout)
                BakedStaticListenerNodeStatsGUI();
            serializedObject.ApplyModifiedProperties();
        }

        void DisplayProgressBarAndCancel()
        {
            BakedStaticListenerNode bakedStaticListener = serializedObject.targetObject as BakedStaticListenerNode;
            bakedStaticListener.phononBaker.DrawProgressBar();
            Repaint();
        }

        public void BakedStaticListenerNodeStatsGUI()
        {
            BakedStaticListenerNode bakedNode = serializedObject.targetObject as BakedStaticListenerNode;
            GUI.enabled = !bakedNode.phononBaker.IsBakeActive();
            bakedNode.UpdateBakedDataStatistics();
            for (int i = 0; i < bakedNode.bakedProbeNames.Count; ++i)
                EditorGUILayout.LabelField(bakedNode.bakedProbeNames[i], (bakedNode.bakedProbeDataSizes[i] / 1000.0f).ToString("0.0") + " KB");
            EditorGUILayout.LabelField("Total Size", (bakedNode.bakedDataSize / 1000.0f).ToString("0.0") + " KB");
            GUI.enabled = true;
        }

        bool bakedStatsFoldout = false;
    }
}