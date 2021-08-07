using System;
using System.Collections;
using UnityEngine;
using ReefEditor.Octrees;

namespace ReefEditor {
    public class RegionLoader : MonoBehaviour {
        public static RegionLoader loader;
        public float loadPercent; 
        public bool aRegionIsLoaded = false;

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

        public void LoadWorld() {
            // very laggy :(
            batchLod = 4;
            placeUsingCoords = true;
            LoadRegion(new Vector3Int(11, 18, 11), new Vector3Int(13, 19, 13));
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
            
            if (aRegionIsLoaded) {
                UnloadRegion();
            }
            aRegionIsLoaded = true;

            this.start = new Vector3Int(Math.Min(start.x, end.x), Math.Min(start.y, end.y), Math.Min(start.z, end.z));
            this.end = new Vector3Int(Math.Max(start.x, end.x), Math.Max(start.y, end.y), Math.Max(start.z, end.z));

            doNextBatch = true;
            
            loadPercent = 0;
            StartCoroutine(RegionLoadCoroutine());
        }

        void UnloadRegion() {

            for (int y = start.y; y <= end.y; y++) {
                for (int z = start.z; z <= end.z; z++) {
                    for (int x = start.x; x <= end.x; x++) {
                        Destroy(loadedBatches[GetLabel(x, y, z)].gameObject);
                    }
                }
            }
            aRegionIsLoaded = false;
        }

        Batch CreateBatchObject(Vector3Int coords) {

            GameObject batchObj = new GameObject($"batch-{coords.x}-{coords.y}-{coords.z}");

            if (placeUsingCoords) {
                batchObj.transform.position = coords * 160;
            }
            
            Batch newBatch = batchObj.AddComponent<Batch>();
            newBatch.OnBatchConstructed += PushQueue;
            newBatch.batchIndex = coords;
            newBatch.Setup(batchLod);

            return newBatch;
        }

        void PushQueue() {
            doNextBatch = true;
        }

        IEnumerator RegionLoadCoroutine() {
            
            int endLabel = GetLabel(end);

            Vector3Int regionSize = end - start + Vector3Int.one;
            int totalBatches = regionSize.x * regionSize.y * regionSize.z;

            loadedBatches = new Batch[totalBatches];

            for (int y = start.y; y <= end.y; y++) {
                for (int z = start.z; z <= end.z; z++) {
                    for (int x = start.x; x <= end.x; x++) {
                        
                        Vector3Int batchCoords = new Vector3Int(x, y, z);

                        string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchCoords.x, batchCoords.y, batchCoords.z);

                        doNextBatch = false;
                        Batch batchComponent = CreateBatchObject(batchCoords);
                        
                        int label = GetLabel(batchCoords);
                        loadedBatches[label] = batchComponent;
                        loadPercent = (float)label / endLabel;

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

            Vector3Int start = new Vector3Int(0, 0, 0);
            Vector3Int end = new Vector3Int(32, 20, 32);
            Vector3Int res = end - start;

            int lastFace = 0;
            Vector3[] vertices = new Vector3[65536];
            Vector3[] normals = new Vector3[65536];
            int[] faceIndices = new int[65536];
            Vector2[] uvs = new Vector2[65536];

            Globals.BakeSimpleMapMaterial();

            for (int y = start.y; y <= end.y; y++) {
                for (int z = start.z; z <= end.z; z++) {
                    for (int x = start.x; x <= end.x; x++) {
                        Vector3Int bIndex = new Vector3Int(x, y, z);
                        int[,,] octrees;

                        if (BatchReadWriter.readWriter.QuickReadBatch(bIndex, out octrees)) {
                            MeshBuilder.builder.ProcessSimpleBatch(vertices, faceIndices, normals, uvs, ref lastFace, octrees, bIndex * 5);
                        }
                    }
                }

                if (lastFace != 0) {
                    MeshBuilder.builder.WrapMeshIntoGameObject(vertices, faceIndices, normals, uvs, ref lastFace);
                }
                
                loadPercent = (float)(y - start.y) / res.y;
                yield return null;
            }

            OnLoadFinish?.Invoke();
            Camera.main.gameObject.SendMessage("OnRegionLoad");
        }

        public void SaveRegion() {
            StartCoroutine(RegionSave());
        }

        IEnumerator RegionSave() {

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

        public int GetLabel(Vector3Int batchIndex) {
            return GetLabel(batchIndex.x, batchIndex.y, batchIndex.z);
        }
        public int GetLabel(int x, int y, int z) {
            int localX = x - start.x;
            int localY = y - start.y;
            int localZ = z - start.z;
            Vector3Int regionSize = end - start + Vector3Int.one;

            return localY * regionSize.x * regionSize.z + localZ * regionSize.x + localX;
        }
        public Batch GetBatchFromLabel(int label) {
            Vector3Int regionSize = end - start + Vector3Int.one;
            int x = label % (regionSize.x);

            label /= regionSize.x;

            int y = label % regionSize.y;

            int z = label / regionSize.y;

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
                // Inverse octree order because weird
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
}