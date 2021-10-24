using ReefEditor.VoxelTech;
using Autodesk.Fbx;
using System.Diagnostics;
using UnityEngine;

namespace ReefEditor {
    public static class ExportFBX {
        public static void ExportMetaspace(VoxelMetaspace metaspace, string fbxFilePath) {

            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (FbxManager fbxManager = FbxManager.Create()) {
                // configure IO settings.
                fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Autodesk.Fbx.Globals.IOSROOT));

                // Export the scene
                using (FbxExporter exporter = FbxExporter.Create(fbxManager, "myExporter")) {

                    // Initialize the exporter.
                    bool status = exporter.Initialize(fbxFilePath + "/reefScene.fbx", -1, fbxManager.GetIOSettings());

                    // Create a new scene to export
                    FbxScene scene = FbxScene.Create(fbxManager, "myScene");

                    // Populate the scene
                    FbxNode rootNode = scene.GetRootNode();
                    rootNode.AddChild(CreateNodeFromMetaspace(fbxManager, metaspace));

                    // Export the scene to the file.
                    exporter.Export(scene);
                }
            }

            sw.Stop();
            UnityEngine.Debug.Log($"Exporting fbx scene took {sw.ElapsedMilliseconds / 1000f}s");
        }

        private static FbxNode CreateNodeFromMetaspace(FbxManager fbxManager, VoxelMetaspace metaspace) {

            if (metaspace.meshes.Length == 0) return null;

            FbxNode metaspaceRootNode = FbxNode.Create(fbxManager, "World Root");

            foreach (VoxelMesh voxelMesh in metaspace.meshes) {

                FbxNode batchRoot = FbxNode.Create(fbxManager, $"Batch {voxelMesh.batchIndex}");

                for (int y = 0; y < voxelMesh.octreeCounts.y; y++) {
                    for (int z = 0; z < voxelMesh.octreeCounts.z; z++) {
                        for (int x = 0; x < voxelMesh.octreeCounts.x; x++) {
                            FbxNode octreeNode = CreateNodeFromUnityMesh(fbxManager, voxelMesh.GetMesh(new Vector3Int(x, y, z)), $"Octree Mesh {x}-{y}-{z}");
                            batchRoot.AddChild(octreeNode);
                        }
                    }
                }

                var localBatchPos = (voxelMesh.batchIndex - VoxelWorld.start) * VoxelWorld.CONTAINERS_PER_SIDE * VoxelWorld.OCTREE_SIDE;
                FbxDouble3 batchPosition = new FbxDouble3(localBatchPos.x, localBatchPos.y, localBatchPos.z);
                batchRoot.LclTranslation.Set(batchPosition);
                metaspaceRootNode.AddChild(batchRoot);
            }

            return metaspaceRootNode;
        }

        private static FbxNode CreateNodeFromUnityMesh(FbxManager fbxManager, Mesh mesh, string nodename) {

            FbxMesh fbxMesh = FbxMesh.Create(fbxManager, mesh.name);

            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < mesh.vertexCount; i++) {
                Vector3 vertex = mesh.vertices[i];
                fbxMesh.SetControlPointAt(new FbxVector4(vertex.x, vertex.y, vertex.z), i);
            }

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++) {

                uint indexCount = mesh.GetIndexCount(submesh);
                int[] indices = mesh.GetIndices(submesh);

                int mod;
                switch (mesh.GetTopology(submesh)) {
                    case MeshTopology.Triangles:
                        mod = 3;
                        break;
                    case MeshTopology.Quads:
                        mod = 4;
                        break;
                    default:
                        UnityEngine.Debug.LogError("Unsupported mesh topology! Aborting export.");
                        throw new System.NotImplementedException("Unsupported mesh topology!");
                }

                int totalPolygons = (int)indexCount / mod;

                for (int p = 0; p < totalPolygons; p++) {

                    fbxMesh.BeginPolygon();
                    fbxMesh.AddPolygon(indices[p * mod]);
                    fbxMesh.AddPolygon(indices[p * mod + 1]);
                    fbxMesh.AddPolygon(indices[p * mod + 2]);

                    if (mod == 4)
                        fbxMesh.AddPolygon(indices[p * mod + 3]);
                    
                    fbxMesh.EndPolygon();
                }
            }

            FbxNode cubeNode = FbxNode.Create(fbxManager, nodename);
            cubeNode.SetNodeAttribute(fbxMesh);
            cubeNode.SetShadingMode(FbxNode.EShadingMode.eFlatShading);
            cubeNode.LclScaling.Set(new FbxColor(1, 1, 1));

            return cubeNode;
        }
    }
}