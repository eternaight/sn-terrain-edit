using System.Collections;
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

        void Awake() {
            metaspace = this;
        }

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
        public byte[] GetVoxel(Vector3Int voxel, Vector3Int octree, Vector3Int batch) {
            VoxelGrid grid = GetVoxelGrid(batch, octree);
            return new byte[] { grid.densityGrid[voxel.x, voxel.y, voxel.z], grid.typeGrid[voxel.x, voxel.y, voxel.z] };
        }

        public void ApplyDensityAction(Brush.BrushStroke stroke) {

            foreach(VoxelMesh mesh in meshes) {
                Vector3 min = (mesh.batchIndex - VoxelWorld.start) * VoxelWorld.OCTREE_SIDE * VoxelWorld.CONTAINERS_PER_SIDE;
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, min, min + Vector3.one * VoxelWorld.OCTREE_SIDE * VoxelWorld.CONTAINERS_PER_SIDE) <= stroke.brushRadius) {
                    mesh.ApplyDensityAction(stroke);
                }
            }
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
                mesh.UpdateFullGrids();
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
