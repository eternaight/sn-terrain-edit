using System;
using System.Collections;
using System.Collections.Generic;
using ReefEditor.ContentLoading;
using ReefEditor.Streaming;
using ReefEditor.VoxelEditing;
using UnityEngine;

namespace ReefEditor {
    public class VoxelMetaspace : MonoBehaviour {
        public static VoxelMetaspace instance;

        private OctNode[] rootNodes;
        private BlocktypeMaterial[] blocktypes;

        public TerrainStreamer streamer;
        public BrushMaster brushMaster;

        public bool regionLoaded = false;

        private const int BIGGEST_NODE = 32;
        private const int TREES_PER_BATCH = 125;
        private const int TREES_PER_BATCH_SIDE = 5;

        public event Action OnRegionLoaded;
        public event Action OnRegionExported;

        private Vector3Int _octreeMin;
        private Vector3Int _octreeMax;
        public Vector3Int OctreeCounts {
            get {
                return _octreeMax - _octreeMin + Vector3Int.one;
            }
        }
        public Vector3Int RealSize {
            get {
                return OctreeCounts * BIGGEST_NODE;
            }
        }

        private void Awake() {
            instance = this;
            streamer = new TerrainStreamer();
            brushMaster = new BrushMaster();
        }
        private void Start() {
            brushMaster.Start();
        }

        public static void InitiateRegionLoad(Vector3Int choiceA, Vector3Int choiceB) {
            var reader = new MetaspaceReader();
            reader.inputA = choiceA;
            reader.inputB = choiceB;
            EditorManager.GetLoading().OnQueueEmpty += instance.OnRegionLoaded;
            EditorManager.GetLoading().AddLoader(reader);
            EditorManager.GetLoading().AddLoader(instance.streamer);

        }
        public static void InitiateRegionExport(int exportMode) {
            var writer = new MetaspaceWriter();
            writer.mode = exportMode;
            EditorManager.GetLoading().OnQueueEmpty += instance.OnRegionExported;
            EditorManager.GetLoading().AddLoader(writer);
        }

        private void Initialize(Vector3Int octreeMin, Vector3Int octreeMax) {
            _octreeMin = octreeMin;
            _octreeMax = octreeMax;

            var regionSize = OctreeCounts;
            rootNodes = new OctNode[regionSize.x * regionSize.y * regionSize.z];

            transform.localPosition = octreeMin * -1 * BIGGEST_NODE;
            streamer.Initialize(octreeMin, octreeMax, BIGGEST_NODE);
        }
        private void Clear() {
            rootNodes = null;
            streamer.ClearRegion();
        }
        public void ReceiveMaterials(BlocktypeMaterial[] mats) {
            blocktypes = mats;
        }

        public VoxelData GetOctnodeVoxel(Vector3Int voxel, int maxSearchHeight) {
            if (OctreeExists(voxel / BIGGEST_NODE)) {
                var node = rootNodes[GetLabel(voxel / BIGGEST_NODE)];
                return node.GetVoxel(voxel, maxSearchHeight);
            }
            return null;
        }
        public OctNode GetOctnode(Vector3Int tree) {
            if (OctreeExists(tree)) {
                return rootNodes[GetLabel(tree)];
            }
            return null;
        }

        public IEnumerable<int> OctreeLabelsOfBatch(Vector3Int batchId) {
            Vector3Int start = batchId * TREES_PER_BATCH_SIDE, end = (batchId + Vector3Int.one) * TREES_PER_BATCH_SIDE;
            for (int x = start.x; x < end.x; x++) {
                for (int y = start.y; y < end.y; y++) {
                    for (int z = start.z; z < end.z; z++) {
                        yield return GetLabel(x, y, z);
                    }
                }
            }
        }
        public IEnumerator<OctNode> OctreesOfBatch(Vector3Int batch) {
            var labels = OctreeLabelsOfBatch(batch);
            foreach (int label in labels) {
                yield return rootNodes[label];
            }
        }
        public IEnumerable<Vector3Int> BatchIndices() {
            Vector3Int batchMin = _octreeMin / TREES_PER_BATCH_SIDE, batchMax = _octreeMax / TREES_PER_BATCH_SIDE + Vector3Int.one;
            for (int x = batchMin.x; x < batchMax.x; x++) {
                for (int y = batchMin.y; y < batchMax.y; y++) {
                    for (int z = batchMin.z; z < batchMax.z; z++) {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public void ApplyVoxelGrid(IVoxelGrid grid) {
            Vector3Int[] bounds = grid.GetBounds();
            Vector3Int octreeMin = bounds[0] / BIGGEST_NODE;
            Vector3Int octreeMax = bounds[1] / BIGGEST_NODE;

            for (int z = octreeMin.z; z <= octreeMax.z; z++) {
                for (int y = octreeMin.y; y <= octreeMax.y; y++) {
                    for (int x = octreeMin.x; x <= octreeMax.x; x++) {
                        var globalIndex = new Vector3Int(x, y, z);
                        if (OctreeExists(globalIndex)) {
                            rootNodes[GetLabel(globalIndex)].MixGrid(grid, 5);
                            streamer.AddOctreeToStream(globalIndex - _octreeMin);
                        }
                    }
                }
            }

            StartCoroutine(streamer.RestartStreaming());
        }

        public byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay, int retryCount = 0) {
            if (retryCount == 32) return 0;

            var voxelandPoint = transform.InverseTransformPoint(hitPoint);
            Vector3Int voxel = new Vector3Int((int)voxelandPoint.x, (int)voxelandPoint.y, (int)voxelandPoint.z);
            Vector3Int treeId = voxel / BIGGEST_NODE + _octreeMin;

            if (OctreeExists(treeId)) {
                byte sample = rootNodes[GetLabel(treeId)].GetVoxel(voxel, 5).blocktype;
                if (sample != 0) {
                    return sample;
                }
            }

            float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
            Vector3 newPoint = cameraRay.GetPoint(newDistance);
            return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
        }

        public bool VoxelExists(int x, int y, int z) {
            return x >= _octreeMin.x * BIGGEST_NODE && x < (_octreeMax.x + 1) * BIGGEST_NODE &&
                    y >= _octreeMin.y * BIGGEST_NODE && y < (_octreeMax.y + 1) * BIGGEST_NODE &&
                    z >= _octreeMin.z * BIGGEST_NODE && z < (_octreeMax.z + 1) * BIGGEST_NODE;
        }
        public bool OctreeExists(Vector3Int index) {
            return (index.x >= _octreeMin.x && index.x <= _octreeMax.x &&
                    index.y >= _octreeMin.y && index.y <= _octreeMax.y &&
                    index.z >= _octreeMin.z && index.z <= _octreeMax.z);
        }

        public int CountBatches() {
            var a = OctreeCounts / TREES_PER_BATCH_SIDE;
            return a.x * a.y * a.z;
        }

        private int GetLabel(Vector3Int globalBatchIndex) {
            return GetLabel(globalBatchIndex.x, globalBatchIndex.y, globalBatchIndex.z);
        }
        private int GetLabel(int x, int y, int z) {
            int localX = x - _octreeMin.x;
            int localY = y - _octreeMin.y;
            int localZ = z - _octreeMin.z;
            return Utilities.LinearIndex(localX, localY, localZ, OctreeCounts);
        }

        public Material GetMaterialForBlocktype(int blocktype) => blocktypes[blocktype].MakeMaterial();
        public bool CheckBlocktypeDefined(int blocktype) {
            var gg = blocktypes[blocktype];
            if (gg == null) return false;
            return gg.ExistsInGame;
        }

        private class MetaspaceReader : ILoader {

            // loading parameters
            public Vector3Int inputA;
            public Vector3Int inputB;

            private bool loadingDone = false;
            private float loadingProgress;
            private string loadingState;

            public string GetTaskDescription() => loadingState;

            public float GetTaskProgress() => loadingProgress;

            public bool IsFinished() => loadingDone;

            public void StartLoading() {
                LoadRegion(inputA, inputB);
            }

            private void LoadRegion(Vector3Int pointA, Vector3Int pointB) {

                var start = new Vector3Int(Mathf.Min(pointA.x, pointB.x), Mathf.Min(pointA.y, pointB.y), Mathf.Min(pointA.z, pointB.z));
                var end = new Vector3Int(Mathf.Max(pointA.x, pointB.x), Mathf.Max(pointA.y, pointB.y), Mathf.Max(pointA.z, pointB.z));
                Debug.Log($"Reading {start} to {end}");

                loadingDone = false;
                instance.StartCoroutine(RegionLoadCoroutine(start, end));
            }
            private IEnumerator RegionLoadCoroutine(Vector3Int minBatch, Vector3Int maxBatch) {

                if (instance.regionLoaded) {
                    instance.Clear();
                }
                instance.Initialize(minBatch * 5, maxBatch * 5 + Vector3Int.one * 4);

                instance.regionLoaded = true;

                var batchCount = instance.CountBatches();
                int i = 0;
                foreach (Vector3Int batchId in instance.BatchIndices()) {
                    loadingProgress = ++i / batchCount;
                    loadingState = $"Reading batch {batchId}";
                    var nodes = BatchReadWriter.ReadBatch(batchId);
                    var labels = instance.OctreeLabelsOfBatch(batchId);

                    foreach (int label in labels) {
                        nodes.MoveNext();
                        instance.rootNodes[label] = nodes.Current;
                        yield return null;
                    }
                }

                loadingDone = true;
            }
        }

        private class MetaspaceWriter : ILoader {

            public int mode;
            private bool finished;

            public string GetTaskDescription() {
                switch (mode) {
                    case 0:
                        return "Writing optoctree files...";
                    case 1:
                        return "Writing world patch...";
                    case 2:
                        return "Writing FBX file...";
                    default:
                        return "Unexpected export option!";
                }
            }

            public float GetTaskProgress() {
                return 0;
            }

            public bool IsFinished() => finished;

            public void StartLoading() {
                finished = false;
                instance.StartCoroutine(ExportCoroutine(mode));
            }

            private IEnumerator ExportCoroutine(int mode) {
                switch (mode) {
                    case 0:
                        yield return BatchReadWriter.WriteOptOctreesCoroutine(instance);
                        break;
                    case 1:
                        yield return BatchReadWriter.WriteOctreePatchCoroutine(instance);
                        break;
                    case 2:
                        yield return ExportFBX.ExportMetaspaceAsync(instance.streamer, EditorManager.instance.batchOutputPath);
                        break;
                    default:
                        Debug.LogError("Unexpected export mode!");
                        break;
                }

                finished = true;
                yield break;
            }
        }
    }
}
