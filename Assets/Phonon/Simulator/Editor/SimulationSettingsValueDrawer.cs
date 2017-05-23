//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEditor;
using UnityEngine;

namespace Phonon
{
    //
    // SimulationSettingsValueDrawer
    // Custom property drawer for SimulationSettingsValue.
    //

    [CustomPropertyDrawer(typeof(SimulationSettingsValue))]
    public class SimulationSettingsDrawer : PropertyDrawer
    {
        //
        //	Returns the overall height of the drawing area.
        //
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 208;
        }

        //
        //	Draws the property.
        //
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = 16f;

            if (position.x <= 0)
            {
                position.x += 4f;
                position.width -= 8f;
            }

            EditorGUI.PropertyField(position, property.FindPropertyRelative("Duration"), new GUIContent("Duration (s)"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("AmbisonicsOrder"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("MaxSources"));
            position.y += 24f;
            EditorGUI.HelpBox(position, "Realtime Settings", MessageType.None);
            position.y += 24f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("RealtimeRays"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("RealtimeSecondaryRays"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("RealtimeBounces"));
            position.y += 24f;
            EditorGUI.HelpBox(position, "Baking Settings", MessageType.None);
            position.y += 24f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("BakeRays"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("BakeSecondaryRays"));
            position.y += 16f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("BakeBounces"));
        }
    }
}