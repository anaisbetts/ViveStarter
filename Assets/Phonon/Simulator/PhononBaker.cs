using UnityEngine;

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Phonon
{
    public enum BakingMode
    {
        StaticSource,
        StaticListener,
        Reverb,
    }

    //
    //    BakeStatus
    //    Possible states the bake process can be in.
    //
    public enum BakeStatus
    {
        Ready,
        InProgress,
        Complete
    }

    public class PhononBaker
    {
        public void BakeEffectThread()
        {
            BakingSettings bakeSettings;
            bakeSettings.bakeConvolution = bakeConvolution ? Bool.True : Bool.False;
            bakeSettings.bakeParametric = bakeParameteric ? Bool.True : Bool.False;

            foreach (ProbeBox probeBox in duringBakeProbeBoxes)
            {
                if (cancelBake)
                    return;

                if (probeBox == null)
                {
                    Debug.LogError("Probe Box specified in list of Probe Boxes to bake is null.");
                    continue;
                }

                if (probeBox.probeBoxData == null || probeBox.probeBoxData.Length == 0)
                {
                    Debug.LogError("Skipping probe box, because probes have not been generated for it.");
                    continue;
                }

                IntPtr probeBoxPtr = IntPtr.Zero;
                try
                {
                    PhononCore.iplLoadProbeBox(probeBox.probeBoxData, probeBox.probeBoxData.Length, ref probeBoxPtr);
                    probeBoxBakingCurrently++;
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                if (duringBakeMode == BakingMode.Reverb)
                    PhononCore.iplBakeReverb(duringBakePhononContainer.Environment().GetEnvironment(), probeBoxPtr, 
                        bakeSettings, bakeCallback);
                else if (duringBakeMode == BakingMode.StaticListener)
                    PhononCore.iplBakeStaticListener(duringBakePhononContainer.Environment().GetEnvironment(), probeBoxPtr, 
                        duringBakeSphere, Common.ConvertString(duringBakeIdentifier), bakeSettings, bakeCallback);
                else if (duringBakeMode == BakingMode.StaticSource)
                    PhononCore.iplBakePropagation(duringBakePhononContainer.Environment().GetEnvironment(), probeBoxPtr, 
                        duringBakeSphere, Common.ConvertString(duringBakeIdentifier), bakeSettings, bakeCallback);

                if (cancelBake)
                    return;

                int probeBoxSize = PhononCore.iplSaveProbeBox(probeBoxPtr, null);
                probeBox.probeBoxData = new byte[probeBoxSize];
                PhononCore.iplSaveProbeBox(probeBoxPtr, probeBox.probeBoxData);

                string bakeDataString = "__reverb__";
                if (duringBakeMode == BakingMode.StaticListener)
                    bakeDataString = bakedListenerPrefix + duringBakeIdentifier;
                else if (duringBakeMode == BakingMode.StaticSource)
                    bakeDataString = duringBakeIdentifier;

                int probeBoxEffectSize = PhononCore.iplGetBakedDataSizeByName(probeBoxPtr, Common.ConvertString(bakeDataString));
                probeBox.UpdateProbeDataMapping(bakeDataString, probeBoxEffectSize);

                PhononCore.iplDestroyProbeBox(ref probeBoxPtr);
            }

            bakeStatus = BakeStatus.Complete;
        }

        public void BeginBake(ProbeBox[] probeBoxes, BakingMode bakingMode, string identifier = default(string), 
            Sphere sphere = default(Sphere))
        {
            oneBakeActive = true;
            bakeStatus = BakeStatus.InProgress;
            duringBakeProbeBoxes = probeBoxes;
            duringBakeMode = bakingMode;
            duringBakeIdentifier = identifier;
            duringBakeSphere = sphere;

#if UNITY_EDITOR
            totalProbeBoxes = duringBakeProbeBoxes.Length;
#endif

            if (probeBoxes.Length == 0)
                Debug.LogError("Probe Box component not attached or no probe boxes selected for baking.");

            // Initialize environment
            try
            {
                duringBakePhononManager = GameObject.FindObjectOfType<PhononManager>();
                if (duringBakePhononManager == null)
                    throw new Exception("Phonon Manager Settings object not found in the scene! Click Window > Phonon");

                bool initializeRenderer = false;
                duringBakePhononManager.Initialize(initializeRenderer);
                duringBakePhononContainer = duringBakePhononManager.PhononManagerContainer();
                duringBakePhononContainer.Initialize(initializeRenderer, duringBakePhononManager);

                if (duringBakePhononContainer.Scene().GetScene() == IntPtr.Zero)
                    throw new Exception("Make sure to pre-export the scene before baking.");
            }
            catch (Exception e)
            {
                bakeStatus = BakeStatus.Complete;
                Debug.LogError(e.Message);
                return;
            }

            bakeCallback = new PhononCore.BakeProgressCallback(AdvanceProgress);

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            bakeCallbackPointer = Marshal.GetFunctionPointerForDelegate(bakeCallback);
            bakeCallbackHandle = GCHandle.Alloc(bakeCallbackPointer);
            GC.Collect();
#endif

            bakeThread = new Thread(new ThreadStart(BakeEffectThread));
            bakeThread.Start();
        }

        public void EndBake()
        {
            if (bakeThread != null)
                bakeThread.Join();

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            if (bakeCallbackHandle.IsAllocated)
                bakeCallbackHandle.Free();
#endif

            if (duringBakePhononManager)
                duringBakePhononManager.Destroy();

            if (duringBakePhononContainer != null)
                duringBakePhononContainer.Destroy();

            duringBakePhononManager = null;
            duringBakeProbeBoxes = null;
#if UNITY_EDITOR
            totalProbeBoxes = 0;
            probeBoxBakingProgress = .0f;
#endif
            probeBoxBakingCurrently = 0;
            bakeStatus = BakeStatus.Ready;
            oneBakeActive = false;
        }

        public void CancelBake()
        {
            cancelBake = true;                      // Ensures partial baked data is not serialized and that bake is properly cancelled for multiple probe boxes.
            PhononCore.iplCancelBake();
            EndBake();
            oneBakeActive = false;
            cancelBake = false;
        }

        public bool IsBakeActive()
        {
            return oneBakeActive;
        }

        public BakeStatus GetBakeStatus()
        {
            return bakeStatus;
        }

        void AdvanceProgress(float bakeProgressFraction)
        {
#if UNITY_EDITOR
            probeBoxBakingProgress = bakeProgressFraction;
#endif
        }

        public void DrawProgressBar()
        {
#if UNITY_EDITOR
            if (bakeStatus != BakeStatus.InProgress)
                return;

            float progress = probeBoxBakingProgress + .01f; //Adding an offset because progress bar when it is exact 0 has some non-zero progress.
            int progressPercent = Mathf.FloorToInt(Mathf.Min(progress * 100.0f, 100.0f));
            string progressString = "Baking " + probeBoxBakingCurrently + "/" + totalProbeBoxes + " Probe Box (" + progressPercent.ToString() + "% complete)";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, progressString);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Cancel Bake"))
            {
                Debug.Log("CANCELLED: Baking.");
                CancelBake();
            }
            EditorGUILayout.EndHorizontal();
#endif
        }

        //Bake Settings
        static public bool oneBakeActive = false;
        int probeBoxBakingCurrently = 0;

#if UNITY_EDITOR
        int totalProbeBoxes = 0;
        float probeBoxBakingProgress = .0f;
#endif

        bool bakeConvolution = true;
        bool bakeParameteric = false;
        bool cancelBake = false;

        BakeStatus bakeStatus = BakeStatus.Ready;
        PhononManager duringBakePhononManager = null;
        PhononManagerContainer duringBakePhononContainer = null;

        ProbeBox[] duringBakeProbeBoxes = null;
        Sphere duringBakeSphere;
        BakingMode duringBakeMode;
        string duringBakeIdentifier;
        string bakedListenerPrefix = "__staticlistener__";

        Thread bakeThread;
        PhononCore.BakeProgressCallback bakeCallback;

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        IntPtr bakeCallbackPointer;
        GCHandle bakeCallbackHandle;
#endif

    }
}

