using System;
using System.Collections;
using UnityEngine;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class VoxelWorld : MonoBehaviour {
        // constants
        public static int LEVEL_OF_DETAIL { get; private set; } //0-5 -> 32-1 side
        public const int OCTREE_SIDE = 32;
        public const int CONTAINERS_PER_SIDE = 5;

        public static int RESOLUTION {
            get {
                return (int)Mathf.Pow(2, 5 - LEVEL_OF_DETAIL);
            }
        }

        // singleton
        private static VoxelWorld world;

        // public static fields
        public static Vector3Int start;
        public static Vector3Int end;

        // private fields
        bool aRegionIsLoaded = false;
        VoxelMetaspace metaspace;

        // events
        public static event Action OnRegionLoaded;
        public static event Action OnRegionExported;

        // Mono methods
        void Awake() {
            world = this;
            metaspace = GetComponent<VoxelMetaspace>();
        }

        // public static methods
        public static void LoadSimpleMap() {
            world.StartCoroutine(world.SimpleMapLoadCoroutine());
        }
        private IEnumerator SimpleMapLoadCoroutine() {

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
                
                yield return null;
            }

            OnRegionLoaded?.Invoke();
            Camera.main.gameObject.SendMessage("OnRegionLoad");
        }

        public static void LoadWorld() {
            LEVEL_OF_DETAIL = 4;
            // very laggy :(
            LoadRegion(new Vector3Int(0, 0, 0), new Vector3Int(25, 20, 25));
        }
        public static void LoadSingleBatch(Vector3Int batch) {
            LEVEL_OF_DETAIL = 0;
            LoadRegion(batch, batch);
        }

        public static void LoadRegion(Vector3Int _start, Vector3Int _end) {
            
            if (world.aRegionIsLoaded) {
                world.metaspace.Clear();
            }
            world.aRegionIsLoaded = true;

            Debug.Log($"Reading {_start} to {_end}");
            start = new Vector3Int(Math.Min(_start.x, _end.x), Math.Min(_start.y, _end.y), Math.Min(_start.z, _end.z));
            end = new Vector3Int(Math.Max(_start.x, _end.x), Math.Max(_start.y, _end.y), Math.Max(_start.z, _end.z));
            
            world.StartCoroutine(world.RegionLoadCoroutine());
        }
        IEnumerator RegionLoadCoroutine() {

            Vector3Int regionSize = end - start + Vector3Int.one;
            int totalBatches = regionSize.x * regionSize.y * regionSize.z;

            metaspace.Create(totalBatches);

            yield return StartCoroutine(metaspace.RegionReadCoroutine());

            OnRegionLoaded?.Invoke();
            Camera.main.gameObject.SendMessage("OnRegionLoad");
            yield break;
        }

        public static void ExportRegion(bool doPatch, string filename = "terrainPatch") {
            world.StartCoroutine(world.ExportRegionCoroutine(doPatch, filename));
        }
        IEnumerator ExportRegionCoroutine(bool doPatch, string filename) {
            if (doPatch) {
                yield return StartCoroutine(BatchReadWriter.readWriter.WriteOctreePatchCoroutine(filename, metaspace));
            } else {
                foreach (VoxelMesh batch in metaspace.meshes) {
                    BatchReadWriter.readWriter.WriteOptoctrees(batch.batchIndex, batch.nodes);
                    yield return null;
                }
            }

            OnRegionExported();
        }
        
        public static byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay, int retryCount= 0) {
            // batch -> octree -> voxel
            if (retryCount == 32) return 0;

            Vector3Int regionSize = end - start + Vector3Int.one;

            // batch
            Vector3Int batchOffset = new Vector3Int(Mathf.FloorToInt(hitPoint.x / 160), Mathf.FloorToInt(hitPoint.z / 160) * regionSize.x, Mathf.FloorToInt(hitPoint.y / 160) * regionSize.x * regionSize.z);
            Vector3Int batchIndex = start + batchOffset;
            VoxelMesh batch = world.metaspace.meshes[batchOffset.x + batchOffset.z * regionSize.x + batchOffset.y * regionSize.z * regionSize.x];

            Vector3 _local = hitPoint - batchOffset * RESOLUTION * OCTREE_SIDE; 
            int x = (int)_local.x / RESOLUTION;
            int y = (int)_local.y / RESOLUTION;
            int z = (int)_local.z / RESOLUTION;

            byte type = batch.octreeContainers[Globals.LinearIndex(x, y, z, 5)].SampleBlocktype(hitPoint);

            if (type == 0) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);

                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            return type;
        }

        // Voxel Metaspace access

    }
}