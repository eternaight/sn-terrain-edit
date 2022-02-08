using System;
using System.Collections;
using UnityEngine;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class VoxelWorld : MonoBehaviour {
        // constants
        // LOD. 0-5 lod => 32-1 resolution
        private static int _lod;
        // this defines the in-game size of the meshes
        private const int OCTREE_SIDE = 32;
        private const int TREES_PER_BATCH = 125;
        private const int TREES_PER_BATCH_SIDE = 5;

        // This defines the count of voxels (count = resolution^3)
        public static int GridResolution(int lod) => (int)Mathf.Pow(2, 5 - _lod);

        private static VoxelWorld world;

        public static bool regionLoaded = false;


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

        VoxelMetaspace metaspace;

        public static event Action OnRegionLoaded;
        public static event Action OnRegionExported;

        void Awake() {
            world = this;
            metaspace = GetComponent<VoxelMetaspace>();
        }

        public static void LoadWorld() {
            _lod = 4;
            // very laggy :(
            LoadRegion(new Vector3Int(0, 0, 0), new Vector3Int(25, 20, 25));
        }
        public static void LoadSingleBatch(Vector3Int batch) {
            _lod = 0;
            LoadRegion(batch, batch);
        }

        public static void LoadRegion(Vector3Int pointA, Vector3Int pointB) {
            
            if (regionLoaded) {
                world.metaspace.Clear();
            }
            regionLoaded = true;

            var start = new Vector3Int(Math.Min(pointA.x, pointB.x), Math.Min(pointA.y, pointB.y), Math.Min(pointA.z, pointB.z));
            var end = new Vector3Int(Math.Max(pointA.x, pointB.x), Math.Max(pointA.y, pointB.y), Math.Max(pointA.z, pointB.z));
            Debug.Log($"Reading {start} to {end}");

            world.StartCoroutine(world.RegionLoadCoroutine(start, end));
        }
        IEnumerator RegionLoadCoroutine(Vector3Int minBatch, Vector3Int maxBatch) {

            metaspace.Create(minBatch * 5, maxBatch * 5 + Vector3Int.one * 4);

            yield return StartCoroutine(metaspace.RegionReadCoroutine());

            OnRegionLoaded?.Invoke();
            Globals.RedrawBoundaryPlanes();
            Camera.main.gameObject.SendMessage("OnRegionLoad");
        }

        public static void ExportRegion(int mode) {
            world.StartCoroutine(world.ExportRegionCoroutine(mode));
        }
        private IEnumerator ExportRegionCoroutine(int mode) {
            switch (mode) {
                case 0:
                    yield return StartCoroutine(BatchReadWriter.readWriter.WriteOptOctreesCoroutine(metaspace));
                    break;
                case 1:
                    yield return StartCoroutine(BatchReadWriter.readWriter.WriteOctreePatchCoroutine(metaspace));
                    break;
                case 2:
                    yield return StartCoroutine(ExportFBX.ExportMetaspaceAsync(metaspace, Globals.instance.batchOutputPath));
                    break;
                default:
                    Debug.LogError("Unexpected export mode!");
                    break;
            }

            OnRegionExported?.Invoke();
        }
        
        public static byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay) {
            return VoxelMetaspace.metaspace.SampleBlocktype(hitPoint, cameraRay);
        }

        public static Coroutine StartMetaspaceRegenerate(int tasksDone, int tasksTotal) => world.StartCoroutine(VoxelMetaspace.metaspace.RegenerateAllMeshesCoroutine());

        public static Vector3Int RealSize() {
            return VoxelMetaspace.metaspace.RealSize;
        }

        public static int CountBatches() {
            var a = VoxelMetaspace.metaspace.OctreeCounts / TREES_PER_BATCH_SIDE;
            return a.x * a.y * a.z;
        }
    }
}