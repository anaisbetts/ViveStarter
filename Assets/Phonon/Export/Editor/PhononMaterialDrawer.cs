//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEditor;
using UnityEngine;

namespace Phonon
{

    //
    // PhononMaterialDrawer
    // Custom property drawer for PhononMaterialValue.
    //

    [CustomPropertyDrawer(typeof(PhononMaterialValue))]
    public class PhononMaterialDrawer : PropertyDrawer
    {
        //
        // Returns the total height of the field.
        //
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 64f;
        }

        //
        // Draws the field.
        //
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = 16f;

            if (position.x <= 0)
            {
                position.x += 4f;
                position.width -= 8f;
            }

            EditorGUI.PropertyField(position, property.FindPropertyRelative("LowFreqAbsorption"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("MidFreqAbsorption"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("HighFreqAbsorption"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("Scattering"));
        }
    }
}