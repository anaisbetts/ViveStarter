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
    // PhononMixerInspector
    // Custom inspector for the PhononMixer component.
    //
    [CustomEditor(typeof(PhononListener))]
    public class PhononListenerInspector : Editor
    {
        //
        // Draws the inspector.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PhononGUI.SectionHeader("Mixer Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("acceleratedMixing"));

            PhononGUI.SectionHeader("Rendering Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("indirectBinauralEnabled"));

            if (serializedObject.FindProperty("acceleratedMixing").boolValue && serializedObject.FindProperty("indirectBinauralEnabled").boolValue)
                EditorGUILayout.HelpBox("The binaural settings on Phonon Source will be ignored.", MessageType.Info);

            if (!serializedObject.FindProperty("acceleratedMixing").boolValue)
            {
                PhononGUI.SectionHeader("Reverb Settings");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableReverb"));

                if (serializedObject.FindProperty("enableReverb").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reverbSimulationType"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dryMixFraction"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reverbMixFraction"));

                    PhononListener phononListener = serializedObject.targetObject as PhononListener;
                    if (phononListener.reverbSimulationType == ReverbSimulationType.BakedReverb)
                    {
                        BakedReverbGUI();
                        bakedStatsFoldout = EditorGUILayout.Foldout(bakedStatsFoldout, "Baked Reverb Statistics", true);
                        if (bakedStatsFoldout)
                            BakedReverbStatsGUI();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        //
        // GUI for BakedReverb
        //
        public void BakedReverbGUI()
        {
            PhononGUI.SectionHeader("Baked Reverb Settings");

            PhononListener bakedReverb = serializedObject.targetObject as PhononListener;
            GUI.enabled = !bakedReverb.phononBaker.IsBakeActive();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useAllProbeBoxes"));
            if (!serializedObject.FindProperty("useAllProbeBoxes").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("probeBoxes"), true);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Bake Reverb"))
            {
                Debug.Log("START: Baking reverb effect.");
                bakedReverb.BeginBake();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            DisplayProgressBarAndCancel();

            if (bakedReverb.phononBaker.GetBakeStatus() == BakeStatus.Complete)
            {
                bakedReverb.EndBake();
                Repaint();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("COMPLETED: Baking reverb effect.");
            }
        }

        public void BakedReverbStatsGUI()
        {
            PhononListener bakedReverb = serializedObject.targetObject as PhononListener;
            GUI.enabled = !bakedReverb.phononBaker.IsBakeActive();
            bakedReverb.UpdateBakedDataStatistics();
            for (int i = 0; i < bakedReverb.bakedProbeNames.Count; ++i)
                EditorGUILayout.LabelField(bakedReverb.bakedProbeNames[i], (bakedReverb.bakedProbeDataSizes[i] / 1000.0f).ToString("0.0") + " KB");
            EditorGUILayout.LabelField("Total Size", (bakedReverb.bakedDataSize / 1000.0f).ToString("0.0") + " KB");
            GUI.enabled = true;
        }

        void DisplayProgressBarAndCancel()
        {
            PhononListener bakedReverb = serializedObject.targetObject as PhononListener;
            bakedReverb.phononBaker.DrawProgressBar();
            Repaint();
        }

        bool bakedStatsFoldout = false;
    }
}