﻿using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace ReefEditor.Streaming {
    public class TerrainStreamer : ILoader {
        public OctreeMesh[][][] meshes;

        private Vector3Int _octreeMin;
        private Vector3Int _octreeMax;
        private int _biggestNode;

        private Queue<StreamItem> queue;
        private StreamItem itemNow;
        private int queueStartCount;

        public TerrainStreamer() {
            queue = new Queue<StreamItem>();
        }

        public void Initialize(Vector3Int octreeMin, Vector3Int octreeMax, int octreeSize) {

            _octreeMin = octreeMin;
            _octreeMax = octreeMax;
            _biggestNode = octreeSize;

            var size = octreeMax - octreeMin + Vector3Int.one;
            meshes = new OctreeMesh[size.z][][];

            for (int z = 0; z < size.z; z++) {
                meshes[z] = new OctreeMesh[size.y][];
                for (int y = 0; y < size.y; y++) {
                    meshes[z][y] = new OctreeMesh[size.x];
                    for (int x = 0; x < size.x; x++) {
                        var index = new Vector3Int(x, y, z);
                        var container = new GameObject().AddComponent<OctreeMesh>();
                        container.Setup(VoxelMetaspace.instance.transform, index + octreeMin, octreeSize);

                        queue.Enqueue(new StreamItem() { octree = index, level = 5 });
                        meshes[z][y][x] = container;
                    }
                }
            }
        }

        public void ClearRegion() {
            List<GameObject> childrenToDelete = new List<GameObject>();
            foreach (Transform tf in VoxelMetaspace.instance.transform) {
                if (tf.GetComponent<OctreeMesh>()) {
                    childrenToDelete.Add(tf.gameObject);
                }
            }
            childrenToDelete.ForEach(child => GameObject.Destroy(child));

        }

        public IEnumerable<OctreeMesh> IterateMeshes() {
            for (int z = _octreeMin.z; z <= _octreeMax.z; z++) {
                for (int y = _octreeMin.y; y <= _octreeMax.y; y++) {
                    for (int x = _octreeMin.x; x <= _octreeMax.x; x++) {
                        yield return meshes[z][y][x];
                    }
                }
            }
        }

        public IEnumerator RestartStreaming() {
            while (queue.Count > 0) {
                itemNow = queue.Dequeue();
                meshes[itemNow.octree.z][itemNow.octree.y][itemNow.octree.x].ReloadMesh(itemNow.level, _biggestNode);
                yield return null;
            }
        }

        public void AddOctreeToStream(Vector3Int localIndex) {
            queue.Enqueue(new StreamItem() { octree = localIndex, level = 5 });
        }

        public void StartLoading() {
            queueStartCount = queue.Count;
            VoxelMetaspace.instance.StartCoroutine(RestartStreaming());
        }

        public bool IsFinished() {
            return queue.Count == 0;
        }

        public float GetTaskProgress() {
            return 1 - (float)queue.Count / queueStartCount;
        }

        public string GetTaskDescription() {
            return $"Streaming batch {(itemNow.octree + _octreeMin) / 5}";
        }

        private struct StreamItem {
            public Vector3Int octree;
            public int level;
        }
    }
}
