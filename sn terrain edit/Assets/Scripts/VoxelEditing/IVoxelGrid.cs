using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public interface IVoxelGrid {
        bool GetMask(Vector3Int voxel);
        OctNodeData BlendVoxel(OctNodeData data, Vector3Int voxel);
        Vector3Int[] GetBounds();
    }
}