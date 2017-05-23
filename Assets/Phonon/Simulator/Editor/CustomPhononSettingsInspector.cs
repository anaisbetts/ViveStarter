//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEditor;

namespace Phonon
{

    //
    // CustomPhononSettingsInspector
    // Custom inspector for custom phonon settings component.
    //

    [CustomEditor(typeof(CustomPhononSettings))]
    public class CustomPhononSettingsInspector : Editor
    {

        //
        // Draws the inspector.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PhononGUI.SectionHeader("Simulation Setting");
            serializedObject.FindProperty("rayTracerOption").enumValueIndex = EditorGUILayout.Popup("Raytracer Options", serializedObject.FindProperty("rayTracerOption").enumValueIndex, optionsRayTracer);

            PhononGUI.SectionHeader("Renderer Setting");
            serializedObject.FindProperty("convolutionOption").enumValueIndex = EditorGUILayout.Popup("Convolution Options", serializedObject.FindProperty("convolutionOption").enumValueIndex, optionsConvolution);

            PhononGUI.SectionHeader("OpenCL Setting");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useOpenCL"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("computeDeviceOption"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numComputeUnits"));

            EditorGUILayout.HelpBox("This is an experimental feature. Please contact the developers to get relevant documentation to use Custom Phonon Settings feature.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }

        string[] optionsRayTracer = new string[] { "Phonon", "Embree", "Radeon Rays", "Custom" };
        string[] optionsConvolution = new string[] { "Phonon", "TrueAudioNext" };
    }
}