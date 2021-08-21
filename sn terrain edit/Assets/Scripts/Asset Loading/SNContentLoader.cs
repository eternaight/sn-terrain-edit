using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor.ContentLoading {
    public class SNContentLoader : MonoBehaviour {
        public static SNContentLoader instance;
        public BlocktypeMaterial[] blocktypesData;
        Dictionary<string, List<int>> materialBlocktypes;
        public bool contentLoaded = false;
        public bool busyLoading = false;

        // TODO: remove later and rework into some loader task system
        public bool updateMeshesOnLoad;

        private static float lastFrame;

        public float loadProgress;
        public string loadState;

        void Awake() {
            instance = this;
        } 

        public IEnumerator LoadContent() {

            busyLoading = true;
            loadState = "Loading material names";
            loadProgress = 0;
            int totalTasks = 12;
            if (updateMeshesOnLoad) totalTasks += 3;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            LoadMaterialNames();
            sw.Stop();

            Debug.Log($"Loaded material names in {sw.ElapsedMilliseconds}ms");
            loadProgress = 1f / totalTasks;
            loadState = "Getting assets";
            yield return null;

            sw.Restart();
            List<AssetStudio.Texture2D> textureAssets = new List<AssetStudio.Texture2D>();
            List<AssetStudio.Material> materialAssets = new List<AssetStudio.Material>();
            yield return StartCoroutine(GetAssets(textureAssets, materialAssets));
            sw.Stop();

            Debug.Log($"Got assets in {sw.ElapsedMilliseconds}ms");
            loadProgress = 4f / totalTasks;
            loadState = "Setting materials";
            yield return null;
        
            sw.Restart();
            yield return StartCoroutine(SetMaterials(materialAssets.ToArray()));
            loadProgress = 8f / totalTasks;
            loadState = "Setting textures";
            yield return StartCoroutine(SetTextures(textureAssets.ToArray()));
            sw.Stop();
            Debug.Log($"Set assets in {sw.ElapsedMilliseconds}ms");
            contentLoaded = true;
            
            if (updateMeshesOnLoad) {
                VoxelWorld.StartMetaspaceRegenerate(12, totalTasks);
                while (VoxelWorld.loadInProgress) {
                    loadProgress = VoxelWorld.loadingProgress;
                    loadState = VoxelWorld.loadingState;
                    yield return null;
                }
                updateMeshesOnLoad = false;
            }
            busyLoading = false;
        }

        IEnumerator GetAssets(List<AssetStudio.Texture2D> textureAssets, List<AssetStudio.Material> materialAssets) {

            string bundleName = "\\resources.assets";
            string resourcesPath = Globals.instance.resourcesSourcePath + bundleName;
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

                    if (Time.time - lastFrame >= 1.0f) {
                        lastFrame = Time.time;
                        yield return null;
                    }
                }
            }

            assetManager.Clear();
        }

        void LoadMaterialNames() {
            string combinedString = Resources.Load<TextAsset>(Globals.instance.blocktypeStringsFilename).text;
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

                List<int> blocktypes;
                if (!materialBlocktypes.TryGetValue(nondecoName, out blocktypes)) {
                    blocktypes = new List<int>();
                    materialBlocktypes.Add(nondecoName, blocktypes);
                }
                blocktypes.Add(blocktype);
                
                blocktypesData[blocktype] = new BlocktypeMaterial(materialName, nondecoName, blocktype);
            }
        }

        IEnumerator SetTextures(AssetStudio.Texture2D[] textureAssets) {
            foreach (AssetStudio.Texture2D textureAsset in textureAssets) {
                List<int> blocktypes = new List<int>();
                if (IsTextureNeeded(textureAsset.m_PathID, out blocktypes)) {
                    byte[] image_data = textureAsset.image_data.GetData();

                    Texture2D newtexture = new Texture2D(textureAsset.m_Width, textureAsset.m_Height, (TextureFormat)((int)textureAsset.m_TextureFormat), true);
                    newtexture.LoadRawTextureData(image_data);
                    newtexture.Apply();

                    int textureType = DetermineTextureType(textureAsset.m_Name);
                    foreach(int b in blocktypes) {
                        blocktypesData[b].SetTexture(textureType, textureAsset.m_PathID, newtexture);
                    }
                }
                if (Time.time - lastFrame >= 1.0f) {
                    lastFrame = Time.time;
                    yield return null;
                }
            }
        }

        IEnumerator SetMaterials(AssetStudio.Material[] materialAssets) {
            foreach (AssetStudio.Material materialAsset in materialAssets) {
                List<long> texturePathIDs = new List<long>();
                foreach(KeyValuePair<string, AssetStudio.UnityTexEnv> pair in materialAsset.m_SavedProperties.m_TexEnvs) {
                    texturePathIDs.Add(pair.Value.m_Texture.m_PathID);
                }

                if (materialBlocktypes.ContainsKey(materialAsset.m_Name)) {
                    foreach(int blocktype in materialBlocktypes[materialAsset.m_Name]) {
                        blocktypesData[blocktype].pathIDs.AddRange(texturePathIDs);
                    }
                }

                if (Time.time - lastFrame >= 1.0f) {
                    lastFrame = Time.time;
                    yield return null;
                }
            }
        }

        public static Material GetMaterialForType(int b) {
            if (instance.contentLoaded && instance.blocktypesData[b] != null && instance.blocktypesData[b].ExistsInGame) {
                return instance.blocktypesData[b].MakeMaterial();
            }

            Material colorMat = new Material(Globals.GetBatchMat());
            colorMat.name = $"Material of type {b}";
            colorMat.SetColor("_Color", Globals.ColorFromType(b));
            return colorMat;
        }

        bool IsTextureNeeded(long pathID, out List<int> blocktypes) {

            blocktypes = new List<int>();

            for(int i = 0; i < 255; i++) {
                if (blocktypesData[i] != null) { 
                    if (blocktypesData[i].pathIDs.Contains(pathID) && pathID != 0) {
                        blocktypes.Add(i);
                    }
                }
            }

            return blocktypes.Count > 0;
        } 
        
        static int DetermineTextureType(string name) {
            string lowercaseName = name.ToLower();
            if (lowercaseName.Contains("sig")) return 2;
            if (lowercaseName.Contains("normal")) return 1;
            if (lowercaseName.Contains("mormal")) return 1;
            return 0;
        } 
    }

    public class BlocktypeMaterial {
        public string originalName;
        public string prettyName;
        public int blocktype;
        public List<long> pathIDs = new List<long>();
        public Texture2D[] textures;

        public BlocktypeMaterial(string _originalName, string _prettyName, int _blocktype) {
            originalName = _originalName;
            prettyName = _prettyName;
            blocktype = _blocktype;
        }

        public Texture2D MainTexture {
            get {
                if (textures[3] != null) {
                    return textures[2];
                }
                return textures[1];
            }
        }
        public Texture2D SideTexture {
            get {
                return textures[5];
            }
        }

        public bool ExistsInGame {
            get { 
                return textures != null;
            }
        }

        public void SetTexture(int type, long pathID, Texture2D texture) {
            if (textures == null) {
                textures = new Texture2D[6];
            }
            // flowing lava materials have 8 textures
            if (pathIDs.Count > 6) {
                if (type == 0) {
                    if (textures[2] != null) {
                        textures[5] = texture;
                    } else {
                        textures[2] = texture;
                    }
                } else if (type == 1) {
                    if (textures[0] != null) {
                        textures[3] = texture;
                    } else {
                        textures[0] = texture;
                    }
                }
                // skipping SIG, noise and flow because irrelevant now
            } else {
                textures[pathIDs.IndexOf(pathID)] = texture;
            }
        } 

        public Material MakeMaterial() {
            Material mat;
            if (textures != null && textures[3] != null) {
                mat = new Material(Globals.instance.batchCappedMat);

                mat.SetTexture("_MainTex", textures[2]);
                mat.SetTexture("_NormalMap", textures[0]);
                mat.SetTexture("_SideTex", textures[5]);
                mat.SetTexture("_SideNormalMap", textures[3]);
            } else {
                mat = new Material(Globals.instance.batchMat);

                mat.SetTexture("_MainTex", textures[1]);
                mat.SetTexture("_NormalMap", textures[0]);
            }
            
            mat.name = originalName;

            return mat;
        }
    }
}