//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEditor;

namespace Phonon
{

    //
    // PhononMaterialInspector
    // Custom inspector for PhononMaterial components.
    //

    [CustomEditor(typeof(PhononMaterial))]
    [CanEditMultipleObjects]
    public class PhononMaterialInspector : Editor
    {
        //
        // Draws the inspector.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PhononGUI.SectionHeader("Material Preset");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Preset"));

            if (serializedObject.FindProperty("Preset").enumValueIndex < 11)
            {
                PhononMaterialValue actualValue = ((PhononMaterial)target).Value;
                actualValue.CopyFrom(PhononMaterialPresetList.PresetValue(serializedObject.FindProperty("Preset").enumValueIndex));
            }

            else
            {
                PhononGUI.SectionHeader("Custom Material");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Value"));
            }

            EditorGUILayout.Space();

            // Save changes.
            serializedObject.ApplyModifiedProperties();
        }
    }
}