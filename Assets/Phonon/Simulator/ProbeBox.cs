//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phonon
{
    //
    // ProbeBox
    // Represents a Phonon Box.
    //

    [AddComponentMenu("Phonon/Probe Box")]
    public class ProbeBox : MonoBehaviour
    {
        void OnDrawGizmosSelected()
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.localScale);

            float PROBE_DRAW_SIZE = .1f;
            Gizmos.color = Color.yellow;
            if (probeSpherePoints != null)
                for (int i = 0; i < probeSpherePoints.Length / 3; ++i)
                {
                    UnityEngine.Vector3 center = new UnityEngine.Vector3(probeSpherePoints[3 * i + 0], 
                        probeSpherePoints[3 * i + 1], -probeSpherePoints[3 * i + 2]);
                    Gizmos.DrawCube(center, new UnityEngine.Vector3(PROBE_DRAW_SIZE, PROBE_DRAW_SIZE, PROBE_DRAW_SIZE));
                }
            Gizmos.color = oldColor;
        }

        public void GenerateProbes()
        {
            ProbePlacementParameters placementParameters;
            placementParameters.placement = placementStrategy;
            placementParameters.maxNumTriangles = maxNumTriangles;
            placementParameters.maxOctreeDepth = maxOctreeDepth;
            placementParameters.horizontalSpacing = horizontalSpacing;
            placementParameters.heightAboveFloor = heightAboveFloor;

            // Initialize environment
            PhononManager duringProbePhononManager;
            PhononManagerContainer duringProbePhononContainer;
            try
            {
                duringProbePhononManager = FindObjectOfType<PhononManager>();
                if (duringProbePhononManager == null)
                    throw new Exception("Phonon Manager Settings object not found in the scene! Click Window > Phonon");

                bool initializeRenderer = false;
                duringProbePhononManager.Initialize(initializeRenderer);
                duringProbePhononContainer = duringProbePhononManager.PhononManagerContainer();
                duringProbePhononContainer.Initialize(initializeRenderer, duringProbePhononManager);

                if (duringProbePhononContainer.Scene().GetScene() == IntPtr.Zero)
                    Debug.LogError("Scene not found. Make sure to pre-export the scene.");
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return;
            }

            // Create bounding box for the probe.
            IntPtr probeBox = IntPtr.Zero;
            Box boundingBox;
            boundingBox.minCoordinates = Common.ConvertVector(gameObject.transform.position);
            boundingBox.maxCoordinates = Common.ConvertVector(gameObject.transform.position);
            boundingBox.minCoordinates.x -= gameObject.transform.localScale.x / 2;
            boundingBox.minCoordinates.y -= gameObject.transform.localScale.y / 2;
            boundingBox.minCoordinates.z -= gameObject.transform.localScale.z / 2;
            boundingBox.maxCoordinates.x += gameObject.transform.localScale.x / 2;
            boundingBox.maxCoordinates.y += gameObject.transform.localScale.y / 2;
            boundingBox.maxCoordinates.z += gameObject.transform.localScale.z / 2;

            PhononCore.iplCreateProbeBox(duringProbePhononContainer.Scene().GetScene(), boundingBox, placementParameters, null, ref probeBox);

            int numProbes = PhononCore.iplGetProbeSpheres(probeBox, null);
            probeSpherePoints = new float[3*numProbes];
            probeSphereRadii = new float[numProbes];

            Sphere[] probeSpheres = new Sphere[numProbes];
            PhononCore.iplGetProbeSpheres(probeBox, probeSpheres);
            for (int i = 0; i < numProbes; ++i)
            {
                probeSpherePoints[3 * i + 0] = probeSpheres[i].centerx;
                probeSpherePoints[3 * i + 1] = probeSpheres[i].centery;
                probeSpherePoints[3 * i + 2] = probeSpheres[i].centerz;
                probeSphereRadii[i] = probeSpheres[i].radius;
            }

            // Save probe box into searlized data;
            int probeBoxSize = PhononCore.iplSaveProbeBox(probeBox, null);
            probeBoxData = new byte[probeBoxSize];
            PhononCore.iplSaveProbeBox(probeBox, probeBoxData);

            if (duringProbePhononContainer.Scene().GetScene() != IntPtr.Zero)
                Debug.Log("Generated " + probeSpheres.Length + " probes for game object " + gameObject.name + ".");

            // Cleanup.
            PhononCore.iplDestroyProbeBox(ref probeBox);
            duringProbePhononManager.Destroy();
            duringProbePhononContainer.Destroy();
            ClearProbeDataMapping();

            // Redraw scene view for probes to show up instantly.
#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        public void DeleteBakedDataByName(string name)
        {
            IntPtr probeBox = IntPtr.Zero;
            try
            {
                PhononCore.iplLoadProbeBox(probeBoxData, probeBoxData.Length, ref probeBox);
                PhononCore.iplDeleteBakedDataByName(probeBox, Common.ConvertString(name));
                UpdateProbeDataMapping(name, -1);

                int probeBoxSize = PhononCore.iplSaveProbeBox(probeBox, null);
                probeBoxData = new byte[probeBoxSize];
                PhononCore.iplSaveProbeBox(probeBox, probeBoxData);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

        }

        public void UpdateProbeDataMapping(string name, int size)
        {
            int index = probeDataName.IndexOf(name);
            if (size == -1 && index >= 0)
            {
                probeDataName.RemoveAt(index);
                probeDataNameSizes.RemoveAt(index);
            }
            else if (index == -1)
            {
                probeDataName.Add(name);
                probeDataNameSizes.Add(size);
            }
            else
            {
                probeDataNameSizes[index] = size;
            }
        }

        void ClearProbeDataMapping()
        {
            probeDataName.Clear();
            probeDataNameSizes.Clear();
        }

        // Public members.
        public ProbePlacementStrategy placementStrategy;

        [Range(.1f, 50f)]
        public float horizontalSpacing = 5f;

        [Range(.1f, 20f)]
        public float heightAboveFloor = 1.5f;

        [Range(1, 1024)]
        public int maxNumTriangles = 64;

        [Range(1, 10)]
        public int maxOctreeDepth = 2;

        public byte[] probeBoxData = null;
        public float[] probeSpherePoints = null;
        public float[] probeSphereRadii = null;

        public List<string> probeDataName = new List<string>();
        public List<int> probeDataNameSizes = new List<int>();
    }
}
