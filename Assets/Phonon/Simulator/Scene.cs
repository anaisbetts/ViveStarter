//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phonon
{
    public class Scene
    {
        public IntPtr GetScene()
        {
            return scene;
        }

        public Error Export(ComputeDevice computeDevice, SimulationSettings simulationSettings, PhononMaterialValue defaultMaterial, GlobalContext globalContext)
        {
            var error = Error.None;

            var objects = GameObject.FindObjectsOfType<PhononGeometry>();

            simulationSettings.sceneType = SceneType.Phonon;    // Scene type should always be Phonon when exporting the scene.
            error = PhononCore.iplCreateScene(globalContext, computeDevice.GetDevice(), simulationSettings,
                objects.Length, ref scene);
            if (error != Error.None)
            {
                throw new Exception("Unable to create scene for export (" + objects.Length.ToString() +
                    " materials): [" + error.ToString() + "]");
            }

            var materials = new Material[objects.Length];

            for (var i = 0; i < objects.Length; ++i)
            {
                materials[i].absorptionHigh = defaultMaterial.HighFreqAbsorption;
                materials[i].absorptionMid = defaultMaterial.MidFreqAbsorption;
                materials[i].absorptionLow = defaultMaterial.LowFreqAbsorption;
                materials[i].scattering = defaultMaterial.Scattering;

                objects[i].GetMaterial(ref materials[i]);
                PhononCore.iplSetSceneMaterial(scene, i, materials[i]);
            }

            var totalNumVertices = 0;
            var numVertices = new int[objects.Length];

            var totalNumTriangles = 0;
            var numTriangles = new int[objects.Length];

            for (var i = 0; i < objects.Length; ++i)
            {
                numVertices[i] = objects[i].GetNumVertices();
                totalNumVertices += numVertices[i];

                numTriangles[i] = objects[i].GetNumTriangles();
                totalNumTriangles += numTriangles[i];
            }

            var staticMesh = IntPtr.Zero;
            error = PhononCore.iplCreateStaticMesh(scene, totalNumVertices, totalNumTriangles, ref staticMesh);
            if (error != Error.None)
            {
                throw new Exception("Unable to create static mesh for export (" + totalNumVertices.ToString() +
                    " vertices, " + totalNumTriangles.ToString() + " triangles): [" + error.ToString() + "]");
            }

            var vertices = new Vector3[totalNumVertices];
            var vertexOffset = 0;

            var triangles = new Triangle[totalNumTriangles];
            var triangleOffset = 0;

            var materialIndices = new int[totalNumTriangles];

            for (var i = 0; i < objects.Length; ++i)
            {
                objects[i].GetGeometry(vertices, vertexOffset, triangles, triangleOffset);

                for (var j = 0; j < numTriangles[i]; ++j)
                    materialIndices[triangleOffset + j] = i;

                vertexOffset += numVertices[i];
                triangleOffset += numTriangles[i];
            }

            PhononCore.iplSetStaticMeshVertices(scene, staticMesh, vertices);
            PhononCore.iplSetStaticMeshTriangles(scene, staticMesh, triangles);
            PhononCore.iplSetStaticMeshMaterials(scene, staticMesh, materialIndices);

            PhononCore.iplFinalizeScene(scene, null);

#if UNITY_EDITOR
            if (!Directory.Exists(Application.streamingAssetsPath))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "StreamingAssets");
#endif

            error = PhononCore.iplSaveFinalizedScene(scene, Common.ConvertString(SceneFileName()));
            if (error != Error.None)
            {
                throw new Exception("Unable to save scene to " + SceneFileName() + " [" + error.ToString() + "]");
            }

            PhononCore.iplDestroyStaticMesh(ref staticMesh);
            PhononCore.iplDestroyScene(ref scene);

            Debug.Log("Scene exported to " + SceneFileName() + ".");

            return error;
        }

        public Error DumpToObj(ComputeDevice computeDevice, SimulationSettings simulationSettings, PhononMaterialValue defaultMaterial, GlobalContext globalContext)
        {
            var error = Error.None;

            var objects = GameObject.FindObjectsOfType<PhononGeometry>();

            simulationSettings.sceneType = SceneType.Phonon;    // Scene type should always be Phonon when exporting the scene.
            error = PhononCore.iplCreateScene(globalContext, computeDevice.GetDevice(), simulationSettings,
                objects.Length, ref scene);
            if (error != Error.None)
            {
                throw new Exception("Unable to create scene for export (" + objects.Length.ToString() +
                    " materials): [" + error.ToString() + "]");
            }

            var materials = new Material[objects.Length];

            for (var i = 0; i < objects.Length; ++i)
            {
                materials[i].absorptionHigh = defaultMaterial.HighFreqAbsorption;
                materials[i].absorptionMid = defaultMaterial.MidFreqAbsorption;
                materials[i].absorptionLow = defaultMaterial.LowFreqAbsorption;
                materials[i].scattering = defaultMaterial.Scattering;

                objects[i].GetMaterial(ref materials[i]);
                PhononCore.iplSetSceneMaterial(scene, i, materials[i]);
            }

            var totalNumVertices = 0;
            var numVertices = new int[objects.Length];

            var totalNumTriangles = 0;
            var numTriangles = new int[objects.Length];

            for (var i = 0; i < objects.Length; ++i)
            {
                numVertices[i] = objects[i].GetNumVertices();
                totalNumVertices += numVertices[i];

                numTriangles[i] = objects[i].GetNumTriangles();
                totalNumTriangles += numTriangles[i];
            }

            var staticMesh = IntPtr.Zero;
            error = PhononCore.iplCreateStaticMesh(scene, totalNumVertices, totalNumTriangles, ref staticMesh);
            if (error != Error.None)
            {
                throw new Exception("Unable to create static mesh for export (" + totalNumVertices.ToString() +
                    " vertices, " + totalNumTriangles.ToString() + " triangles): [" + error.ToString() + "]");
            }

            var vertices = new Vector3[totalNumVertices];
            var vertexOffset = 0;

            var triangles = new Triangle[totalNumTriangles];
            var triangleOffset = 0;

            var materialIndices = new int[totalNumTriangles];

            for (var i = 0; i < objects.Length; ++i)
            {
                objects[i].GetGeometry(vertices, vertexOffset, triangles, triangleOffset);

                for (var j = 0; j < numTriangles[i]; ++j)
                    materialIndices[triangleOffset + j] = i;

                vertexOffset += numVertices[i];
                triangleOffset += numTriangles[i];
            }

            PhononCore.iplSetStaticMeshVertices(scene, staticMesh, vertices);
            PhononCore.iplSetStaticMeshTriangles(scene, staticMesh, triangles);
            PhononCore.iplSetStaticMeshMaterials(scene, staticMesh, materialIndices);

            PhononCore.iplFinalizeScene(scene, null);

#if UNITY_EDITOR
            if (!Directory.Exists(Application.streamingAssetsPath))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "StreamingAssets");
#endif

            PhononCore.iplDumpSceneToObjFile(scene, Common.ConvertString(ObjFileName()));

            PhononCore.iplDestroyStaticMesh(ref staticMesh);
            PhononCore.iplDestroyScene(ref scene);

            Debug.Log("Scene dumped to " + ObjFileName() + ".");

            return error;
        }

        public Error Create(ComputeDevice computeDevice, SimulationSettings simulationSettings, GlobalContext globalContext)
        {
            string fileName = SceneFileName();
            if (!File.Exists(fileName))
                return Error.Fail;

            var error = PhononCore.iplLoadFinalizedScene(globalContext, simulationSettings, Common.ConvertString(fileName),
                computeDevice.GetDevice(), null, ref scene);

            return error;
        }

        public void Destroy()
        {
            if (scene != IntPtr.Zero)
                PhononCore.iplDestroyScene(ref scene);
        }

        static string SceneFileName()
        {
            var fileName = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().name) + ".phononscene";
            var streamingAssetsFileName = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
			      var tempFileName = Path.Combine(Application.temporaryCachePath, fileName);

            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

      			try
			      {
	              var streamingAssetLoader = new WWW(streamingAssetsFileName);
                while (!streamingAssetLoader.isDone);
				        if (string.IsNullOrEmpty(streamingAssetLoader.error))
				        {
					          var assetData = streamingAssetLoader.bytes;
					          using (var dataWriter = new BinaryWriter(new FileStream(tempFileName, FileMode.Create)))
					          {
						            dataWriter.Write(assetData);
						            dataWriter.Close();
					          }
				        }
				        else
				        {
					          Debug.Log(streamingAssetLoader.error);
				        }
            }
            catch (Exception exception)
            {
			          Debug.LogError(exception.ToString());
            }
            return tempFileName;
#else
            return streamingAssetsFileName;
#endif
        }

        static string ObjFileName()
        {
            var fileName = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().name) + ".obj";
            return Path.Combine(Application.streamingAssetsPath, fileName);
        }

        IntPtr scene = IntPtr.Zero;
    }
}
