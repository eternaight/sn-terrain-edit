using System.Collections;
using ReefEditor.ContentLoading;
using UnityEngine;

namespace ReefEditor.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public VoxelMesh[] meshes;

        public void Create(int numBatches) {
            meshes = new VoxelMesh[numBatches];

            if (!SNContentLoader.instance.contentLoaded) {
                SNContentLoader.instance.OnContentLoaded += RegenerateMeshes;
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

        public VoxelGrid GetVoxelGrid(Vector3Int globalBatchIndex, Vector3Int containerIndex) {
            return meshes[GetLabel(globalBatchIndex)].GetVoxelGrid(containerIndex);
        }

        public IEnumerator RegionReadCoroutine() {
            // sets + rasterizes all octrees
            foreach (VoxelMesh mesh in meshes) {
                yield return BatchReadWriter.readWriter.ReadBatchCoroutine(mesh.OctreesReadCallback, mesh.batchIndex);
            }

            RegenerateMeshes();
            yield break;
        }

        void RegenerateMeshes() {
            // redistribute full grids
            foreach (VoxelMesh mesh in meshes) {
                mesh.UpdateFullGrids(this);
            }
            // generate meshes
            foreach (VoxelMesh mesh in meshes) {
                mesh.Regenerate();
            }
        }

        void DerasterizeOctrees() {
            // read voxels -> octrees
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
