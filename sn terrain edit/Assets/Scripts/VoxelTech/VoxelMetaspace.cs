﻿using System.Collections;
using System.Collections.Generic;
using ReefEditor.ContentLoading;
using UnityEngine;

namespace ReefEditor.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        public VoxelMesh[] meshes;
        public VoxelMesh this[Vector3Int index] {
            get {
                return meshes[GetLabel(index)];
            }
        }

        // loading fields
        public bool loadInProgress = false;
        public float loadingProgress;
        public string loadingState;

        void Awake() {
            metaspace = this;
        }

        public void Create(int numBatches) {
            meshes = new VoxelMesh[numBatches];

            if (!SNContentLoader.instance.contentLoaded) {
                SNContentLoader.instance.updateMeshesOnLoad = true;
            }

            for (int y = VoxelWorld.start.y; y <= VoxelWorld.end.y; y++) {
                for (int z = VoxelWorld.start.z; z <= VoxelWorld.end.z; z++) {
                    for (int x = VoxelWorld.start.x; x <= VoxelWorld.end.x; x++) {
                        
                        Vector3Int coords = new Vector3Int(x, y, z);
                        VoxelMesh batchComponent = new GameObject($"batch-{x}-{y}-{z}").AddComponent<VoxelMesh>();
                        batchComponent.Create(coords);
                        int label = GetLabel(coords);
                        meshes[label] = batchComponent;
                    }
                }   
            }
        }
        public void Clear() {
            for (int y = VoxelWorld.start.y; y <= VoxelWorld.end.y; y++) {
                for (int z = VoxelWorld.start.z; z <= VoxelWorld.end.z; z++) {
                    for (int x = VoxelWorld.start.x; x <= VoxelWorld.end.x; x++) {
                        Destroy(meshes[GetLabel(x, y, z)].gameObject);
                    }
                }
            }
        }

        public VoxelGrid GetVoxelGrid(Vector3Int globalBatchIndex, Vector3Int containerIndex) => meshes[GetLabel(globalBatchIndex)].GetVoxelGrid(containerIndex);
        public byte[] GetCachedVoxel(Vector3Int voxel, Vector3Int octree, Vector3Int batch) => GetVoxelGrid(batch, octree).GetCachedVoxel(voxel.x, voxel.y, voxel.z);

        public void ApplyDensityAction(Brush.BrushStroke stroke) {
            
            // Cache grids if smooth
            if (stroke.brushMode == BrushMode.Smooth) {
                // cache
                foreach(VoxelMesh mesh in meshes) {
                    Vector3 min = (mesh.batchIndex - VoxelWorld.start) * VoxelWorld.OCTREE_SIDE * VoxelWorld.CONTAINERS_PER_SIDE;
                    if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, min, min + Vector3.one * VoxelWorld.OCTREE_SIDE * VoxelWorld.CONTAINERS_PER_SIDE) <= stroke.brushRadius) {
                        mesh.CacheGridsInsideBrush(stroke);
                    }
                }
            }

            List<VoxelMesh> modifiedMeshes = new List<VoxelMesh>();
            foreach(VoxelMesh mesh in meshes) {
                Vector3 min = (mesh.batchIndex - VoxelWorld.start) * VoxelWorld.OCTREE_SIDE * VoxelWorld.CONTAINERS_PER_SIDE;
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, min, min + Vector3.one * VoxelWorld.OCTREE_SIDE * VoxelWorld.CONTAINERS_PER_SIDE) <= stroke.brushRadius) {
                    mesh.ApplyDensityAction(stroke);
                    modifiedMeshes.Add(mesh);
                }
            }
            foreach(VoxelMesh mesh in modifiedMeshes) {
                mesh.UpdateMeshesAfterBrush(stroke);
            }
        }

        public IEnumerator RegionReadCoroutine() {
            // sets + rasterizes all octrees
            loadInProgress = true;
            float endLabel = GetLabel(VoxelWorld.end) + 1;
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = GetLabel(mesh.batchIndex) / (endLabel * 3);
                loadingState = $"Reading batch {mesh.batchIndex}";
                yield return BatchReadWriter.readWriter.ReadBatchCoroutine(mesh.OctreesReadCallback, mesh.batchIndex);
            }

            yield return RegenerateMeshesCoroutine(1, 3);
        }

        public IEnumerator RegenerateMeshesCoroutine(int tasksDone, int totalTasks) {
            // redistribute full grids
            loadInProgress = true;
            float endLabel = GetLabel(VoxelWorld.end) + 1;
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = (GetLabel(mesh.batchIndex) / (endLabel * totalTasks)) + ((float)tasksDone) / totalTasks;
                loadingState = $"Joining batch {mesh.batchIndex}";
                mesh.UpdateFullGrids();
                yield return null;
            }
            // generate meshes
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = (GetLabel(mesh.batchIndex) / (endLabel * totalTasks)) + ((float)tasksDone + 1) / totalTasks;
                loadingState = $"Creating mesh for {mesh.batchIndex}";
                mesh.Regenerate();
                yield return null;
            }
            loadInProgress = false;
        }

        public static bool VoxelExists(int x, int y, int z) {
            return x >= 1 && x < VoxelWorld.RESOLUTION + 1 && y >= 1 && y < VoxelWorld.RESOLUTION + 1 && z >= 1 && z < VoxelWorld.RESOLUTION + 1;
        }
        public static bool OctreeExists(Vector3Int treeIndex, Vector3Int batchIndex) {
            if (!BatchExists(batchIndex)) return false;
            Vector3Int dimensions = metaspace[batchIndex].octreeCounts;
            return (treeIndex.x >= 0 && treeIndex.x < dimensions.x && treeIndex.y >= 0 && treeIndex.y < dimensions.y && treeIndex.z >= 0 && treeIndex.z < dimensions.z);
        }
        public static bool BatchExists(Vector3Int batchIndex) {
            if (batchIndex.x >= VoxelWorld.start.x && batchIndex.x <= VoxelWorld.end.x
                                                   && batchIndex.y >= VoxelWorld.start.y &&
                                                   batchIndex.y <= VoxelWorld.end.y
                                                   && batchIndex.z >= VoxelWorld.start.z &&
                                                   batchIndex.z <= VoxelWorld.end.z)
            {
                return metaspace[batchIndex].nodes != null;
            }

            return false;
        }


        private int GetLabel(Vector3Int globalBatchIndex) {
            return GetLabel(globalBatchIndex.x, globalBatchIndex.y, globalBatchIndex.z);
        }
        private int GetLabel(int x, int y, int z) {
            int localX = x - VoxelWorld.start.x;
            int localY = y - VoxelWorld.start.y;
            int localZ = z - VoxelWorld.start.z;
            Vector3Int regionSize = VoxelWorld.end - VoxelWorld.start + Vector3Int.one;

            return localY * regionSize.x * regionSize.z + localZ * regionSize.x + localX;
        }
    }
}
