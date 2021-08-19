using System;
using System.Collections;
using UnityEngine;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class VoxelWorld : MonoBehaviour {
        // constants
        // LOD. 0-5 lod => 32-1 resolution
        public static int LEVEL_OF_DETAIL { get; private set; }
        // this defines the in-game size of the meshes
        public const int OCTREE_SIDE = 32;
        // this is the 'resolution' but for batches
        public const int CONTAINERS_PER_SIDE = 5;

        // This defines the count of voxels (count = resolution^3)
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
        public static Vector3Int regionSize {
            get {
                return end - start + Vector3Int.one;
            }
        }
        public static bool aRegionIsLoaded = false;


        // loading fields
        public static bool loadInProgress {
            get {
                return VoxelMetaspace.metaspace.loadInProgress;
            }
        }
        public static float loadingProgress {
            get {
                return VoxelMetaspace.metaspace.loadingProgress;
            }
        }
        public static string loadingState {
            get {
                return VoxelMetaspace.metaspace.loadingState;
            }
        }

        // private fields
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
            
            if (aRegionIsLoaded) {
                world.metaspace.Clear();
            }
            aRegionIsLoaded = true;

            Debug.Log($"Reading {_start} to {_end}");
            start = new Vector3Int(Math.Min(_start.x, _end.x), Math.Min(_start.y, _end.y), Math.Min(_start.z, _end.z));
            end = new Vector3Int(Math.Max(_start.x, _end.x), Math.Max(_start.y, _end.y), Math.Max(_start.z, _end.z));
            
            world.StartCoroutine(world.RegionLoadCoroutine());
        }
        IEnumerator RegionLoadCoroutine() {

            int totalBatches = regionSize.x * regionSize.y * regionSize.z;

            metaspace.Create(totalBatches);

            yield return StartCoroutine(metaspace.RegionReadCoroutine());

            OnRegionLoaded?.Invoke();
            Globals.RedrawBoundaryPlanes();
            Camera.main.gameObject.SendMessage("OnRegionLoad");
        }

        public static void ExportRegion(bool doPatch, string filename = "terrainPatch") {
            world.StartCoroutine(world.ExportRegionCoroutine(doPatch, filename));
        }
        IEnumerator ExportRegionCoroutine(bool doPatch, string filename) {
            if (doPatch) {
                yield return StartCoroutine(BatchReadWriter.readWriter.WriteOctreePatchCoroutine(filename, metaspace));
            } else {
                foreach (VoxelMesh batch in metaspace.meshes) {
                    batch.Write();
                    yield return null;
                }
            }

            OnRegionExported();
        }
        
        public static byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay, int retryCount= 0) {
            // batch -> octree -> voxel
            if (retryCount == 32) return 0;

            // batch
            Vector3Int batchOffset = LocalBatchFromPoint(hitPoint);
            if (VoxelMetaspace.BatchExists(batchOffset + start)) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }
            VoxelMesh batch = world.metaspace[batchOffset + start];

            Vector3 _local = hitPoint - batchOffset * OCTREE_SIDE * CONTAINERS_PER_SIDE; 
            int x = (int)_local.x / OCTREE_SIDE;
            int y = (int)_local.y / OCTREE_SIDE;
            int z = (int)_local.z / OCTREE_SIDE;

            byte type = batch.octreeContainers[Globals.LinearIndex(x, y, z, 5)].SampleBlocktype(hitPoint);

            if (type == 0) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            return type;
        }

        public static Vector3Int LocalBatchFromPoint(Vector3 p) {
            const int batchSide = OCTREE_SIDE * CONTAINERS_PER_SIDE;
            return new Vector3Int(Mathf.FloorToInt(p.x / batchSide), Mathf.FloorToInt(p.y / batchSide), Mathf.FloorToInt(p.z / batchSide));
        }

        public static void StartMetaspaceRegenerate(int tasksDone, int tasksTotal) => world.StartCoroutine(VoxelMetaspace.metaspace.RegenerateMeshesCoroutine(tasksDone, tasksTotal));
    }
}