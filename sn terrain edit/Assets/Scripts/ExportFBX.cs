using ReefEditor.VoxelTech;
using ReefEditor.UI;
using Autodesk.Fbx;
using System.Diagnostics;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace ReefEditor {
    public static class ExportFBX {
        public static IEnumerator ExportMetaspaceAsync(VoxelMetaspace metaspace, string fbxFilePath) {

            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (FbxManager fbxManager = FbxManager.Create()) {
                // configure IO settings.
                fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Autodesk.Fbx.Globals.IOSROOT));

                // Export the scene
                using (FbxExporter exporter = FbxExporter.Create(fbxManager, "myExporter")) {

                    // Initialize the exporter.
                    bool status = exporter.Initialize(fbxFilePath, -1, fbxManager.GetIOSettings());

                    // Create a new scene to export
                    FbxScene scene = FbxScene.Create(fbxManager, "myScene");

                    // Populate the scene
                    FbxNode rootNode = scene.GetRootNode();

                    var node = FbxNode.Create(fbxManager, "World Root");
                    yield return CreateNodeFromMetaspace(fbxManager, metaspace, fbxFilePath, node);
                    rootNode.AddChild(node);

                    // Export the scene to the file.
                    exporter.Export(scene);
                }
            }

            sw.Stop();
            UnityEngine.Debug.Log($"Exporting fbx scene took {sw.ElapsedMilliseconds / 1000f}s");
            yield break;
        }

        private static IEnumerator CreateNodeFromMetaspace(FbxManager fbxManager, VoxelMetaspace metaspace, string fbxFilePath, FbxNode node) {

            if (metaspace.meshes.Length == 0) {
                yield break;
            }

            Dictionary<string, FbxSurfacePhong> materialDict = new Dictionary<string, FbxSurfacePhong>();

            foreach (VoxelMesh voxelMesh in metaspace.meshes) {

                GameObject obj = voxelMesh.gameObject;
                FbxNode octreeNode = FbxNode.Create(fbxManager, obj.name);

                // Create/Add materials
                foreach (Material mat in obj.GetComponent<MeshRenderer>().materials) {
                    if (!materialDict.ContainsKey(mat.name)) {
                        materialDict.Add(mat.name, FbxMaterialFromUnity(fbxManager, mat, fbxFilePath));
                    }

                    octreeNode.AddMaterial(materialDict[mat.name]);
                }

                // Add polygons
                FbxMesh fbxMesh = CreateNodeFromUnityMesh(fbxManager, obj.GetComponent<MeshFilter>().mesh);
                octreeNode.SetNodeAttribute(fbxMesh);
                octreeNode.SetShadingMode(FbxNode.EShadingMode.eFlatShading);
                octreeNode.LclScaling.Set(new FbxColor(1, 1, 1));
                octreeNode.LclTranslation.Set(new FbxDouble3(obj.transform.position.x, obj.transform.position.y, obj.transform.position.z));

                // Update info
                EditorUI.UpdateStatusBar("Exporting FBX mesh...", 0.5f);
                yield return null;

                node.AddChild(octreeNode);
            }

            EditorUI.DisableStatusBar();
        }

        private static FbxMesh CreateNodeFromUnityMesh(FbxManager fbxManager, Mesh mesh) {

            FbxMesh fbxMesh = FbxMesh.Create(fbxManager, mesh.name);

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

                    fbxMesh.BeginPolygon(submesh);
                    fbxMesh.AddPolygon(indices[p * mod]);
                    fbxMesh.AddPolygon(indices[p * mod + 1]);
                    fbxMesh.AddPolygon(indices[p * mod + 2]);

                    if (mod == 4)
                        fbxMesh.AddPolygon(indices[p * mod + 3]);
                    
                    fbxMesh.EndPolygon();
                }
            }

            return fbxMesh;
        }

        private static FbxSurfacePhong FbxMaterialFromUnity(FbxManager fbxManager, Material mat, string exportPath) {
            FbxSurfacePhong fbxMaterial = FbxSurfacePhong.Create(fbxManager, "mat");
            fbxMaterial.Emissive.Set(new FbxDouble3(0, 0, 0));
            fbxMaterial.Ambient.Set(new FbxDouble3(0, 0, 0));
            fbxMaterial.TransparencyFactor.Set(0);
            fbxMaterial.ShadingModel.Set("Phong");
            fbxMaterial.Shininess.Set(0.5);

            //IncludeTextures(fbxManager, mat, exportPath, fbxMaterial);

            return fbxMaterial;
        }

        private static void IncludeTextures(FbxManager fbxManager, Material unityMaterial, string exportPath, FbxSurfacePhong fbxMaterial) {
            switch (unityMaterial.shader.name) {
                case "Shader Graphs/Triplanar":
                    ExportTexture(fbxManager, unityMaterial, exportPath, fbxMaterial, "_MainTex", false);
                    ExportTexture(fbxManager, unityMaterial, exportPath, fbxMaterial, "_NormalMap", true);
                    return;
                case "Shader Graphs/TriplanarCapped":
                    ExportTexture(fbxManager, unityMaterial, exportPath, fbxMaterial, "_MainTex", false);
                    ExportTexture(fbxManager, unityMaterial, exportPath, fbxMaterial, "_NormalMap", true);
                    return;
                case "Universal Render Pipeline/Lit":
                    ExportTexture(fbxManager, unityMaterial, exportPath, fbxMaterial, "_MainTex", false);
                    ExportTexture(fbxManager, unityMaterial, exportPath, fbxMaterial, "_BumpMap", true);
                    return;
                default:
                    throw new System.NotImplementedException("Unsupported material with shader: " + unityMaterial.shader.name);
            }
        }

        private static bool ExportTexture (FbxManager fbxManager, Material unityMaterial, string exportPath, FbxSurfacePhong fbxMaterial, string textureName, bool normal) {
            
            if (!unityMaterial) return false;

            string textureUniqueName = unityMaterial.name + "_" + textureName;

            // TODO: Look for ways to not have this require saving the texture to a file

            var unityTexture = unityMaterial.GetTexture(textureName) as Texture2D;
            if (!unityTexture) return false;

            var renderTexture = RenderTexture.GetTemporary(unityTexture.width, unityTexture.height);
            Graphics.Blit(unityTexture, renderTexture);
            RenderTexture.active = renderTexture;
            var destination = new Texture2D(unityTexture.width, unityTexture.height, TextureFormat.RGB24, true);
            destination.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            destination.Apply();

            // Dirty hack
            var texturePath = Path.Combine(Directory.GetParent(exportPath).FullName, textureUniqueName + ".png");
            File.WriteAllBytes(texturePath, destination.EncodeToPNG());

            FbxFileTexture fbxTexture = FbxFileTexture.Create(fbxManager, textureUniqueName);
            fbxTexture.SetFileName(texturePath);
            fbxTexture.SetTextureUse(FbxTexture.ETextureUse.eStandard);
            fbxTexture.SetMappingType(FbxTexture.EMappingType.eUV);
            fbxTexture.SetMaterialUse(FbxFileTexture.EMaterialUse.eModelMaterial);
            fbxTexture.SetSwapUV(false);
            fbxTexture.SetTranslation(0, 0);
            fbxTexture.SetRotation(0, 0);
            fbxTexture.SetScale(1, 1);

            if (!normal) {
                fbxMaterial.Diffuse.ConnectSrcObject(fbxTexture);
            } 
            else
            {
                fbxMaterial.NormalMap.ConnectSrcObject(fbxTexture);
            }

            return true;
        }
    }
}