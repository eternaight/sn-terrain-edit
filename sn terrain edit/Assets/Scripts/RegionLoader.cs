using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionLoader : MonoBehaviour
{
    public static RegionLoader loader;
    public float loadPercent; 

    public const int octreeSize = 32;
    public Vector3Int start;
    public Vector3Int end;
    public bool doNextBatch = false;

    public Batch[] loadedBatches;

    public event Action OnLoadFinish;
    public event Action OnRegionSaved;

    void Awake() {
        loader = this;
    }

    public void LoadRegion(Vector3Int start, Vector3Int end) {
        
        if (loadPercent != 0) {
            UnloadRegion();
        }

        this.start = start;
        this.end = end;

        doNextBatch = true;
        
        loadPercent = 0;
        StartCoroutine(RegionLoad());
    }

    void UnloadRegion() {

        for (int y = start.y; y <= end.y; y++) {
            for (int z = start.z; z <= end.z; z++) {
                for (int x = start.x; x <= end.x; x++) {
                    Destroy(GameObject.Find($"batch-{x}-{y}-{z}"));
                }
            }
        }
    }

    void CreateBatch(Vector3Int coords, bool queueNextBatch, bool placeUsingCoords = false) {

        GameObject batchObj = new GameObject($"batch-{coords.x}-{coords.y}-{coords.z}");
        Batch newBatch = batchObj.AddComponent<Batch>();
        loadedBatches[GetLabel(coords)] = newBatch;
        newBatch.OnBatchConstructed += PushQueue;

        newBatch.batchIndex = coords;

        newBatch.Setup();
    }

    void PushQueue() {
        doNextBatch = true;
    }

    IEnumerator RegionLoad() {

        int endBatch = GetLabel(end) + 1;

        Vector3Int regionSize = end - start + Vector3Int.one;
        int totalBatches = regionSize.x * regionSize.y * regionSize.z;

        loadedBatches = new Batch[totalBatches];

        for (int y = start.y; y <= end.y; y++) {
            for (int z = start.z; z <= end.z; z++) {
                for (int x = start.x; x <= end.x; x++) {
                    
                    Vector3Int batchCoords = new Vector3Int(x, y, z);
                    string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchCoords.x, batchCoords.y, batchCoords.z);

                    Vector3Int localIndex = (batchCoords - start);
                    int batchNow = GetLabel(batchCoords);

                    doNextBatch = false;
                    CreateBatch(batchCoords, true, true);

                    loadPercent = (float)GetLabel(batchCoords) / endBatch;
                    EditorUI.UpdateStatusBar($"Loading {batchname}", loadPercent);

                    while(!doNextBatch) {
                        yield return null;
                    }
                }
            }   
        }

        if (OnLoadFinish != null)        
            OnLoadFinish();

        Camera.main.gameObject.SendMessage("OnRegionLoad");
    }

    public void SaveRegion() {
        StartCoroutine(RegionSave());
    }

    IEnumerator RegionSave() {
        
        EditorUI.UpdateStatusBar("Exporting batch..", 1);

        for (int y = start.y; y <= end.y; y++) {
            for (int z = start.z; z <= end.z; z++) {
                for (int x = start.x; x <= end.x; x++) {

                    GameObject.Find($"batch-{x}-{y}-{z}").GetComponent<Batch>().Write();
                    
                    while (BatchReadWriter.readWriter.busy) {
                        yield return null;
                    }

                }
            }   
        }

        OnRegionSaved();
        yield return null;
    }

    public byte GetLabel(Vector3Int batchIndex) {
        Vector3Int pos = batchIndex - start;
        return GetLabel(pos.x, pos.y, pos.z);
    }
    public byte GetLabel(int x, int y, int z) {

        Vector3Int regionSize = end - start + Vector3Int.one;
        return (byte)(z * regionSize.y * regionSize.x + y * regionSize.x + x);
    }
    public Batch GetBatchFromLabel(int label) {
        Vector3Int regionSize = end - start + Vector3Int.one;
        int x = label % (regionSize.x);

        int label1 = label / regionSize.x;

        int y = label1 % regionSize.y;

        int label2 = label1 / regionSize.y;

        int z = label2;

        return GetBatchLocal(x, y, z);
    }
    public Batch GetBatchLocal(int x, int y, int z) {
        int label = GetLabel(x, y, z);

        if (label < loadedBatches.Length) {
            return loadedBatches[label];
        }

        return null;
    }
}
