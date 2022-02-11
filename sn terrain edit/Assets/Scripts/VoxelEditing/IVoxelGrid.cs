using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public interface IVoxelGrid {
        bool GetMask(Vector3Int voxel);
        void BlendVoxel(VoxelData data, Vector3Int voxel);
        Vector3Int[] GetBounds();
    }
}