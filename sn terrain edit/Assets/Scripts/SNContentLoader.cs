using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;

public class SNContentLoader : MonoBehaviour
{
    public static SNContentLoader instance;
    string batchMatAddress = "Assets/Materials/Batch-Default.mat";
    public AssetReference batchMatReference;

    public void Awake() {
        instance = this;
    } 

    public static void Load() {

        //Addressables.LoadAssetAsync<Material>(instance.batchMatAddress).Completed += instance.OnBatchMatLoaded;

        // string bundleName = "main.unity_014092a6d84dd5b6d09accbdc07c4c27.bundle";
        // string bundlePath = "C:\\Users\\komet\\Documents\\GitHub\\sn-terrain-edit\\sn terrain edit\\Library\\com.unity.addressables\\aa\\Windows\\StandaloneWindows\\" + bundleName;
        // var myLoadedAssetBundle = AssetBundle.LoadFromFile(bundlePath);

        // Debug.Log(myLoadedAssetBundle.name);
    }

    void OnBatchMatLoaded(AsyncOperationHandle<Material> mat) {
        // Globals.get.batchMat = mat.Result;
        // Debug.Log("Loaded batch mat");
        // Globals.get.GenerateColorMap();
    }
}
