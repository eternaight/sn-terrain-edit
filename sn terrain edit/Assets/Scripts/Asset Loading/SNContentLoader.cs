using System.Collections.Generic;
using UnityEngine;

public class SNContentLoader : MonoBehaviour
{
    public static SNContentLoader instance;
    BlocktypeMaterial[] blocktypesData;
    Dictionary<string, List<int>> materialBlocktypes;
    
    AssetStudio.Texture2D[] loadedTextureAssets;
    AssetStudio.Material[] loadedMaterialAssets;

    void Awake() {
        instance = this;
    } 

    void Start() {
        LoadContent();
    }

    void LoadContent() {

        LoadMaterialNames();
        GetAssets();
        SetMaterials();
        SetTextures();
    }

    void GetAssets() {

        string bundleName = "\\resources.assets";
        string gamePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Subnautica";
        string resourcesPath = string.Concat(gamePath, "\\Subnautica_Data", bundleName);
        string[] files = new string[1];
        files[0] = resourcesPath;

        AssetStudio.AssetsManager assetManager = new AssetStudio.AssetsManager();
        assetManager.LoadFiles(files);

        List<AssetStudio.Texture2D> textureAssets = new List<AssetStudio.Texture2D>();
        List<AssetStudio.Material> materialAssets = new List<AssetStudio.Material>();
        
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

        loadedMaterialAssets = materialAssets.ToArray();
        loadedTextureAssets = textureAssets.ToArray();
    }

    void LoadMaterialNames() {

        string[] lines = System.IO.File.ReadAllLines(Application.dataPath + "//Scripts//Asset Loading//blocktypeStrings.txt");
        blocktypesData = new BlocktypeMaterial[255];
        materialBlocktypes = new Dictionary<string, List<int>>();

        foreach (string line in lines) {
            string[] split1 = line.Split(')');
            int blocktype = 0;
            int.TryParse(split1[0], out blocktype);
            string materialName = split1[1].Substring(1);

            List<int> blocktypes;
            if (!materialBlocktypes.TryGetValue(materialName, out blocktypes)) {
                blocktypes = new List<int>();
                materialBlocktypes.Add(materialName, blocktypes);
            }
            blocktypes.Add(blocktype);

            BlocktypeMaterial entry = new BlocktypeMaterial();
            entry.blocktype = blocktype;
            entry.materialName = materialName;
            blocktypesData[blocktype] = entry;
        }
    }

    void SetTextures() {
        foreach (AssetStudio.Texture2D textureAsset in loadedTextureAssets) {
            List<int> blocktypes = new List<int>();
            if (IsTextureNeeded(textureAsset.m_PathID, out blocktypes)) {
                byte[] image_data = textureAsset.image_data.GetData();
                byte[] image = new byte[0];

                if (textureAsset.m_TextureFormat == AssetStudio.TextureFormat.DXT1) {
                    image = TextureDecoder.DecodeTextureDXT1(image_data, textureAsset.m_Width, textureAsset.m_Height);
                } else if (textureAsset.m_TextureFormat == AssetStudio.TextureFormat.DXT5) {
                    image = TextureDecoder.DecodeTextureDXT5(image_data, textureAsset.m_Width, textureAsset.m_Height);
                }

                Texture2D newtexture = new Texture2D(textureAsset.m_Width, textureAsset.m_Height);

                for (long i = 0; i < image.Length; i += 4) {
                    long pixel_index = i / 4;
                    int x = (int)(pixel_index % textureAsset.m_Width);
                    int y = (int)(pixel_index / textureAsset.m_Width);
                    Color color = new Color(image[i + 2] / 255f, image[i + 1] / 255f, image[i] / 255f, image[i + 3] / 255f);
                    newtexture.SetPixel(x, y, color);
                }
                newtexture.Apply();

                foreach(int b in blocktypes) {
                    blocktypesData[b].SetTexture(DetermineTextureType(textureAsset.m_Name), newtexture);
                }
            }
        }
    }

    void SetMaterials() {
        foreach (AssetStudio.Material materialAsset in loadedMaterialAssets) {
            List<long> texturePathIDs = new List<long>();
            foreach(KeyValuePair<string, AssetStudio.UnityTexEnv> pair in materialAsset.m_SavedProperties.m_TexEnvs) {
                texturePathIDs.Add(pair.Value.m_Texture.m_PathID);
            }

            if (materialBlocktypes.ContainsKey(materialAsset.m_Name)) {
                foreach(int blocktype in materialBlocktypes[materialAsset.m_Name]) {
                    blocktypesData[blocktype].pathIDs.AddRange(texturePathIDs);
                }
            }
        }
    }

    public static Material GetMaterialForType(int b) {
        if (instance.blocktypesData[b] != null) {
            return instance.blocktypesData[b].MakeMaterial();
        }

        Material colorMat = new Material(Globals.GetBatchMat());
        colorMat.name = $"Material of type {b}";;
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
        if (name.Contains("sig")) return 2;
        if (name.Contains("normal")) return 1;
        return 0;
    } 
}

class BlocktypeMaterial {
    public string materialName;
    public int blocktype;
    public List<long> pathIDs = new List<long>();
    public Texture2D[] textures;

    public void SetTexture(int type, Texture2D texture) {
        if (textures == null) {
            textures = new Texture2D[3];
        }
        textures[type] = texture;
    } 

    public Material MakeMaterial() {
        Material mat = new Material(Globals.GetBatchMat());
        
        mat.name = $"Material of type {blocktype}";

        if (textures != null && textures[0] != null)
            mat.SetTexture("_MainTex", textures[0]);
        else {
            mat.SetColor("_Color", Globals.ColorFromType(blocktype));
        }

        if (textures != null && textures[1] != null)
            mat.SetTexture("_BumpMap", textures[1]);

        return mat;
    }
}