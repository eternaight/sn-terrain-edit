using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor.ContentLoading {
    public class SNContentLoader : ILoader {
        public BlocktypeMaterial[] blocktypesData;
        private Dictionary<string, List<int>> materialBlocktypes;
        
        private bool contentLoaded = false;
        private float loadingProgress;
        private string loadingStatus;

        private IEnumerator LoadContent() {

            loadingProgress = 0;
            loadingStatus = "Loading material names...";
            yield return null;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            LoadMaterialNames();
            sw.Stop();

            Debug.Log($"Loaded material names in {sw.ElapsedMilliseconds}ms");
            loadingProgress = 0.2f;
            loadingStatus = "Retrieving assets...";
            yield return null;

            sw.Restart();
            var textureAssets = new List<AssetStudio.Texture2D>();
            var materialAssets = new List<AssetStudio.Material>();
            GetAssets(textureAssets, materialAssets);
            sw.Stop();

            Debug.Log($"Got assets in {sw.ElapsedMilliseconds}ms");
            loadingProgress = 0.6f;
            loadingStatus = "Filling materials...";
            yield return null;
        
            sw.Restart();
            SetMaterials(materialAssets.ToArray());
            yield return null;
            SetTextures(textureAssets.ToArray());
            sw.Stop();

            Debug.Log($"Set assets in {sw.ElapsedMilliseconds}ms");
            VoxelMetaspace.instance.ReceiveMaterials(blocktypesData);
            yield return null;
            contentLoaded = true;
        }

        private void GetAssets(List<AssetStudio.Texture2D> textureAssets, List<AssetStudio.Material> materialAssets) {

            string bundleName = "\\resources.assets";
            string resourcesPath = EditorManager.instance.resourcesSourcePath + bundleName;
            string[] files = { resourcesPath };

            AssetStudio.AssetsManager assetManager = new AssetStudio.AssetsManager();
            assetManager.LoadFiles(files);
            
            foreach (AssetStudio.SerializedFile file in assetManager.assetsFileList) {
                foreach(AssetStudio.Object obj in file.Objects) {
                    AssetStudio.Texture2D textureAsset = obj as AssetStudio.Texture2D;
                    if (textureAsset != null) {
                        textureAssets.Add(textureAsset);
                        continue;
                    }

                    AssetStudio.Material materialAsset = obj as AssetStudio.Material;
                    if (materialAsset != null) {
                        materialAssets.Add(materialAsset);
                    }
                }
            }

            assetManager.Clear();
        }

        private void LoadMaterialNames() {
            string combinedString = Resources.Load<TextAsset>(EditorManager.instance.blocktypeStringsFilename).text;
            string[] lines = combinedString.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            blocktypesData = new BlocktypeMaterial[255];
            materialBlocktypes = new Dictionary<string, List<int>>();

            foreach (string line in lines) {
                string[] split1 = line.Split(')');
                int.TryParse(split1[0], out int blocktype);
                string materialName = split1[1].Substring(1);
                
                string nondecoName = materialName;
                if (materialName.Contains("deco")) {
                    nondecoName = materialName.Split(' ')[0];
                }
                
                if (nondecoName == "Sand01ToCoral15") {
                    nondecoName = "Sand01";
                }

                if (!materialBlocktypes.TryGetValue(nondecoName, out List<int> blocktypes)) {
                    blocktypes = new List<int>();
                    materialBlocktypes.Add(nondecoName, blocktypes);
                }
                blocktypes.Add(blocktype);
                
                blocktypesData[blocktype] = new BlocktypeMaterial(materialName, nondecoName, blocktype);
            }
        }

        private void SetTextures(AssetStudio.Texture2D[] textureAssets) {
            foreach (AssetStudio.Texture2D textureAsset in textureAssets) {
                List<int> blocktypes = new List<int>();
                if (IsTextureNeeded(textureAsset.m_PathID, out blocktypes)) {
                    byte[] image_data = textureAsset.image_data.GetData();

                    Texture2D newtexture = new Texture2D(textureAsset.m_Width, textureAsset.m_Height, (TextureFormat)((int)textureAsset.m_TextureFormat), true);
                    newtexture.LoadRawTextureData(image_data);
                    newtexture.Apply();

                    foreach(int b in blocktypes) {
                        blocktypesData[b].SetTexture(textureAsset.m_PathID, newtexture);
                    }
                }
            }
        }
        private bool IsTextureNeeded(long pathID, out List<int> blocktypes) {

            blocktypes = new List<int>();

            for (int i = 0; i < 255; i++) {
                if (blocktypesData[i] != null) {
                    if (blocktypesData[i].propertyFromPathIDMap.ContainsKey(pathID) && pathID != 0) {
                        blocktypes.Add(i);
                    }
                }
            }

            return blocktypes.Count > 0;
        }

        private void SetMaterials(AssetStudio.Material[] materialAssets) {
            foreach (AssetStudio.Material materialAsset in materialAssets) {

                var texturePathIDs = new Dictionary<long, string>();
                foreach(KeyValuePair<string, AssetStudio.UnityTexEnv> pair in materialAsset.m_SavedProperties.m_TexEnvs) {
                    long pathID = pair.Value.m_Texture.m_PathID;
                    if (pathID != 0 && !texturePathIDs.ContainsKey(pathID))
                        texturePathIDs.Add(pathID, pair.Key);
                }

                if (materialBlocktypes.ContainsKey(materialAsset.m_Name)) {
                    foreach(int blocktype in materialBlocktypes[materialAsset.m_Name]) {
                        blocktypesData[blocktype].propertyFromPathIDMap = texturePathIDs;
                    }
                }
            }
        }

        public Material GetMaterialForType(int b) {
            if (contentLoaded && blocktypesData[b] != null && blocktypesData[b].ExistsInGame) {
                return blocktypesData[b].MakeMaterial();
            }

            Material colorMat = new Material(EditorManager.GetDefaultTriplanarMaterial());
            colorMat.name = $"Material of type {b}";
            colorMat.SetColor("_Color", EditorManager.ColorFromType(b));
            return colorMat;
        }

        public void StartLoading() {
            EditorManager.instance.StartCoroutine(LoadContent());
        }

        public bool IsFinished() => contentLoaded;

        public float GetTaskProgress() => loadingProgress;

        public string GetTaskDescription() => loadingStatus;
    }

    public class BlocktypeMaterial {
        public string originalName;
        public string prettyName;
        public int blocktype;
        private bool useCap;
        public Dictionary<long, string> propertyFromPathIDMap = new Dictionary<long, string>();
        public Texture2D[] textures;

        public BlocktypeMaterial(string _originalName, string _prettyName, int _blocktype) {
            originalName = _originalName;
            prettyName = _prettyName;
            blocktype = _blocktype;
        }

        public Texture2D MainTexture {
            get {
                return textures[0];
            }
        }
        public Texture2D SideTexture {
            get {
                return textures[2];
            }
        }

        public bool ExistsInGame {
            get { 
                return textures != null;
            }
        }

        public void SetTexture(long pathID, Texture2D texture) {
            if (textures == null) {
                textures = new Texture2D[4];
            }
            
            switch (propertyFromPathIDMap[pathID]) {
                case "_MainTex":
                case "_CapTexture":
                    textures[0] = texture;
                    break;
                case "_BumpMap":
                case "_CapBumpMap":
                    textures[1] = texture;
                    break;
                case "_SideTexture":
                    useCap = true;
                    textures[2] = texture;
                    break;
                case "_SideBumpMap":
                    useCap = true;
                    textures[3] = texture;
                    break;
                default:
                    break;
            }
        } 

        public Material MakeMaterial() {
            Material mat;
            if (useCap) {
                mat = new Material(EditorManager.GetCappedTriplanarMaterial());

                mat.SetTexture("_MainTex", textures[0]);
                mat.SetTexture("_NormalMap", textures[1]);
                mat.SetTexture("_SideTex", textures[2]);
                mat.SetTexture("_SideNormalMap", textures[3]);
            } else {
                mat = new Material(EditorManager.GetDefaultTriplanarMaterial());

                mat.SetTexture("_MainTex", textures[0]);
                mat.SetTexture("_NormalMap", textures[1]);
            }
            
            mat.name = originalName;

            return mat;
        }
    }
}