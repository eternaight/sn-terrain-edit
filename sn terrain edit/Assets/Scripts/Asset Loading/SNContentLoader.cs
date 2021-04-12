using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;
using System;

public class SNContentLoader : MonoBehaviour
{
    public static SNContentLoader instance;
    Material[] blocktypeMaterialsLoaded;
    const bool LOUD = true;

    public void Awake() {
        instance = this;
    } 

    void Start() {
        GetTextures();
    }

    void GetTextures() {

        AssetStudio.AssetsManager assManager = new AssetStudio.AssetsManager();
        string bundleName = "\\resources.assets";

        string gamePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Subnautica";
        string bundlePath = string.Concat(gamePath, "\\Subnautica_Data", bundleName);
        string[] files = new string[1];
        files[0] = bundlePath;
        assManager.LoadFiles(files);
        
        Texture2D newtexture = Texture2D.blackTexture;

        foreach (AssetStudio.SerializedFile file in assManager.assetsFileList) {
            foreach(AssetStudio.Object obj in file.Objects) {
                AssetStudio.Texture2D textureAsset = obj as AssetStudio.Texture2D;
                if (textureAsset != null) {
                    if (IsTextureNeeded(textureAsset.m_Name)) {
                        if (textureAsset.m_Name == "lava_01") {
                            byte[] image_data = textureAsset.image_data.GetData();
                            byte[] image = new byte[0];
                            if (textureAsset.m_TextureFormat == AssetStudio.TextureFormat.DXT1) {
                                image = TextureDecoder.DecodeTextureDXT1(image_data, textureAsset.m_Width, textureAsset.m_Height);
                            } else if (textureAsset.m_TextureFormat == AssetStudio.TextureFormat.DXT5) {
                                image = TextureDecoder.DecodeTextureDXT5(image_data, textureAsset.m_Width, textureAsset.m_Height);
                            }

                            newtexture = new Texture2D(textureAsset.m_Width, textureAsset.m_Height);

                            if (LOUD) {
                                print($"Format: {textureAsset.m_TextureFormat}");
                                print($"Size: {textureAsset.m_Width}x{textureAsset.m_Height}");
                                
                                print($"Compressed data Length: {image_data.Length}");
                                print($"Converted data Length: {image.Length}");
                            }

                            for (long i = 0; i < image.Length; i += 4) {
                                long pixel_index = i / 4;
                                int x = (int)(pixel_index % textureAsset.m_Width);
                                int y = (int)(pixel_index / textureAsset.m_Width);
                                Color color = new Color(image[i + 2] / 255f, image[i + 1] / 255f, image[i] / 255f, image[i + 3] / 255f);
                                newtexture.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }
        }

        newtexture.Apply();

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.GetComponent<MeshRenderer>().material.mainTexture = newtexture;

    }

    bool IsTextureNeeded(string name) {
        
        if (name.Contains("kelp")) return true;
        if (name.Contains("coral")) return true;
        if (name.Contains("DGR")) return true;
        if (name.Contains("GP")) return true;
        if (name.Contains("GR")) return true;
        if (name.Contains("JC")) return true;
        if (name.Contains("SS")) return true;
        if (name.Contains("TreaderPath")) return true;
        if (name.Contains("Mesa")) return true;
        if (name.Contains("Land")) return true;
        if (name.Contains("Lava")) return true;
        if (name.Contains("lava")) return true;
        if (name.Contains("LR")) return true;
        if (name.Contains("Rock")) return true;
        if (name.Contains("Sand")) return true;

        return false;
    } 

    public static void Load() {
    }

    void OnBatchMatLoaded(AsyncOperationHandle<Material> mat) {
        Globals.get.batchMat = mat.Result;
        if (Globals.get.batchMat != null) {
            Debug.Log("Successfully loaded content");
        } else {
            Debug.Log("Failed loading content");
        }
        Globals.get.GenerateColorMap();
    }
}
