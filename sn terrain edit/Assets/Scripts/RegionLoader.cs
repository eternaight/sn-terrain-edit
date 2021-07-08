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
    int batchLod;
    bool placeUsingCoords = false;

    void Awake() {
        loader = this;
    }

    public void LoadMap() {
        // very laggy :(
        batchLod = 4;
        placeUsingCoords = true;
        LoadRegion(new Vector3Int(0, 18, 0), new Vector3Int(32, 19, 32));
    }
    public void LoadSimpleMap() {
        placeUsingCoords = true;
        StartCoroutine(SimpleMapLoadCoroutine());
    }

    public void LoadSingleBatch(Vector3Int batch) {
        batchLod = 0;
        placeUsingCoords = false;
        LoadRegion(batch, batch);
    }

    public void LoadRegion(Vector3Int start, Vector3Int end) {
        
        if (loadPercent != 0) {
            UnloadRegion();
        }

        this.start = new Vector3Int(Math.Min(start.x, end.x), Math.Min(start.y, end.y), Math.Min(start.z, end.z));
        this.end = new Vector3Int(Math.Max(start.x, end.x), Math.Max(start.y, end.y), Math.Max(start.z, end.z));

        doNextBatch = true;
        
        loadPercent = 0;
        StartCoroutine(RegionLoad());
    }

    void UnloadRegion() {

        for (int y = start.y; y <= end.y; y++) {
            for (int z = start.z; z <= end.z; z++) {
                for (int x = start.x; x <= end.x; x++) {
                    Destroy(loadedBatches[GetLabel(x, y, z)].gameObject);
                }
            }
        }
    }

    void CreateBatch(Vector3Int coords, bool queueNextBatch) {

        GameObject batchObj = new GameObject($"batch-{coords.x}-{coords.y}-{coords.z}");
        Batch newBatch = batchObj.AddComponent<Batch>();
        loadedBatches[GetLabel(coords)] = newBatch;
        newBatch.OnBatchConstructed += PushQueue;

        newBatch.batchIndex = coords;

        if (placeUsingCoords) {
            batchObj.transform.position = coords * 160;
        }

        newBatch.Setup(batchLod);
    }

    void PushQueue() {
        doNextBatch = true;
    }

    IEnumerator RegionLoad() {
        
        int startBatch = GetLabel(start);
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
                    CreateBatch(batchCoords, true);

                    loadPercent = (float)GetLabel(batchCoords) / (endBatch - startBatch);
                    EditorUI.UpdateStatusBar($"Loading {batchname}", loadPercent);

                    while(!doNextBatch) {
                        yield return null;
                    }
                }
            }   
        }

        OnLoadFinish?.Invoke();
        Camera.main.gameObject.SendMessage("OnRegionLoad");
    }

    public IEnumerator SimpleMapLoadCoroutine() {

        Vector3Int start = new Vector3Int(6, 18, 6);
        Vector3Int end = new Vector3Int(16, 19, 16);
        int startBatch = GetLabel(start);
        int endBatch = GetLabel(end) + 1;

        for (int y = start.y; y <= end.y; y++) {
            for (int z = start.z; z <= end.z; z++) {
                for (int x = start.x; x <= end.x; x++) {
                    Vector3Int batchCoords = new Vector3Int(x, y, z);
                    string batchname = string.Format($"batch {batchCoords.x}-{batchCoords.y}-{batchCoords.z}");

                    SimpleBatch simple = new SimpleBatch();

                    // wait for octree data
                    yield return BatchReadWriter.readWriter.DoReadBatch(simple.OnFinishRead, batchCoords);

                    // maybe dispose of simple?
                    if (simple.Empty) {
                        EditorUI.UpdateStatusBar($"Skipping {batchname}", loadPercent);
                        continue;
                    }

                    // make mesh obj
                    GameObject meshObj = new GameObject(batchname);
                    meshObj.AddComponent<MeshFilter>().mesh = MeshBuilder.builder.GenerateSimpleMesh(simple.GetSimpleOctrees());
                    meshObj.AddComponent<MeshRenderer>().material = Globals.GetBatchMat();
                    meshObj.transform.SetParent(transform);
                    meshObj.transform.position = batchCoords * 160;

                    loadPercent = (float)GetLabel(batchCoords) / (endBatch - startBatch);
                    EditorUI.UpdateStatusBar($"Loading {batchname}", loadPercent);
                    yield return null;
                }
            }
        }

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
        return (byte)(y * regionSize.x * regionSize.z + z * regionSize.x + x);
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

class SimpleBatch {
    Octree[,,] octrees;

    public bool Empty {
        get {
            return octrees == null;
        } 
    }

    public void OnFinishRead(Octree[,,] _octrees) {
        octrees = _octrees;
    }

    public int[,,] GetSimpleOctrees() {
        int[,,] simpleOctrees = new int[5, 5, 5];
        if (octrees != null) {
            for (int z = 0; z < 5; z++) {
                for (int y = 0; y < 5; y++) {
                    for (int x = 0; x < 5; x++) {
                        if (octrees[x, y, z] != null)
                            simpleOctrees[x, y, z] = octrees[x, y, z].GetBlockType();
                    }
                }
            }
        }
        return simpleOctrees;
    }
}
