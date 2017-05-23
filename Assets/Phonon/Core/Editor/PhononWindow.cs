//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Phonon
{
    public class PhononWindow : EditorWindow
    {

        [MenuItem("Window/Phonon")]
        static void Init()
        {
#pragma warning disable 618
            PhononWindow window = GetWindow<PhononWindow>();
            window.title = "Phonon";
            window.Show();
#pragma warning restore 618
        }
        
        void OnEnable()
        {
            autoRepaintOnSceneChange = true;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }
        
        void OnGUI()
        {
            if (phononManager == null || editor == null)
            {
                string name = "Phonon Manager Settings";
                GameObject managerObject = GameObject.Find(name);
                if (managerObject == null)
                {
                    targetObject = new GameObject(name);
                    phononManager = targetObject.AddComponent<PhononManager>();
                    editor = Editor.CreateEditor(phononManager);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
                else
                {
                    phononManager = managerObject.GetComponent<PhononManager>();
                    editor = Editor.CreateEditor(phononManager);
                }
            }

            editor.OnInspectorGUI();
        }

        PhononManager phononManager = null;
        GameObject targetObject = null;
        Editor editor = null;
    }
}