using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public class OctNode {

        // data
        public Vector3Int position;
        public int size;
        public VoxelData voxelData;

        public bool HasChildren { get; private set; }
        public OctNode[] children;

        public OctNode(Vector3Int position, int size) {
            HasChildren = false;
            this.position = position;
            this.size = size;
            voxelData = new VoxelData();
        }

        public byte GetXMajorLocalOctreeIndex() {
            var index = new Vector3Int(position.x / size % 5, position.y / size % 5, position.z / size % 5);
            return (byte)(index.x * 25 + index.y * 5 + index.z);
        }

        public void Subdivide() {

            if (!HasChildren) {
                children = new OctNode[8];

                for (int b = 0; b < 8; b++) {
                    Vector3Int childPosition = position + cornerOffsets[b] * size / 2;
                    children[b] = new OctNode(childPosition, size / 2);
                    children[b].voxelData = new VoxelData(this.voxelData.blocktype, this.voxelData.signedDistance);
                }

                HasChildren = true;
            }
        }
        public void StripChildren() {
            this.voxelData.blocktype = children[0].voxelData.blocktype;
            this.voxelData.signedDistance = children[0].voxelData.signedDistance;
            children = null;
            HasChildren = false;
        }

        /// <summary>
        /// writes data into the octree
        /// </summary>
        public void ReadArray(OctNodeData[] dataarray, int myPos) {

            var data = new OctNodeData(dataarray[myPos]);
            voxelData = new VoxelData(data);

            if (data.childPosition > 0) {
                Subdivide();
                for (int i = 0; i < 8; i++) {
                    children[i].ReadArray(dataarray, data.childPosition + i);
                }
            }
        }

        public void WriteToArray(List<OctNodeData> dataArray) {
            dataArray.Add(voxelData.Encode());
            if (HasChildren) {
                WriteChildrenToArray(dataArray, 1);
            }
        }
        private void WriteChildrenToArray(List<OctNodeData> dataarray, int myPos) {

            if (HasChildren) {

                // get new child index
                int newChildIndex = dataarray.Count;
                dataarray[myPos].childPosition = (ushort)newChildIndex;

                for (int i = 0; i < 8; i++) {
                    dataarray.Add(voxelData.Encode());
                }
                for (int i = 0; i < 8; i++) {
                    children[i].WriteChildrenToArray(dataarray, (newChildIndex + i));
                }
            }
        }

        // About octree heights: 0 = rootx32, 1x16, 2x8, 3x4, 4x2, 5x1. Total of 6 levels.
        public VoxelData GetVoxel(Vector3Int voxel, int height) {
            if (HasChildren && height > 0) {
                for (int i = 0; i < 8; i++) {
                    if (children[i].ContainsVoxel(voxel)) {
                        return children[i].GetVoxel(voxel, height - 1);
                    }
                }
                return null;
            } else {
                return voxelData;
            }
        }
        public bool ContainsVoxel(Vector3Int voxel) {
            return (
                voxel.x >= position.x && voxel.x < position.x + size &&
                voxel.y >= position.y && voxel.y < position.y + size &&
                voxel.z >= position.z && voxel.z < position.z + size
            );
        }

        public bool MixGrid(IVoxelGrid grid, int height) {

            if (height == 0) {
                if (grid.GetMask(position)) {
                    grid.BlendVoxel(voxelData, position);
                }
                return true;
            }
                
            if (!HasChildren) {
                Subdivide();
            }

            //if (IsMonotone()) StripChildren();
            bool childrenMonotone = true;
            bool childrenDataMonotone = true;
            var data0 = children[0].voxelData.Encode();
            for (int b = 0; b < 8; b++) {
                if (!children[b].MixGrid(grid, height - 1))
                    childrenMonotone = false;
                if (children[b].voxelData.blocktype != data0.type)
                    childrenDataMonotone = false;
                if (children[b].voxelData.Encode().density != data0.density)
                    childrenDataMonotone = false;
            }

            if (childrenMonotone && childrenDataMonotone) {
                StripChildren();
            } else {
                voxelData.blocktype = MostCommonChildType();
                voxelData.signedDistance = AverageChildDistance();
            }
            return childrenMonotone && childrenDataMonotone;
        }

        private bool IsMonotone() {

            if (!HasChildren) return false;

            var data = children[0].voxelData.Encode();
            for (int b = 1; b < 8; b++) {
                var _data = children[b].voxelData.Encode();
                if (data.type != _data.type) {
                    return false;
                }
                if (data.density != _data.density) {
                    return false;
                }
            }

            return true;
        }
        private byte MostCommonChildType() {
            if (!HasChildren) return voxelData.blocktype;

            for (int b = 0; b < 8; b++) {
                if (children[b].voxelData.blocktype != 0) {
                    return children[b].voxelData.blocktype;
                }
            }

            return 0;
        }
        private float AverageChildDistance() {
            if (!HasChildren) return voxelData.signedDistance;
            float sum = 0;
            int realCount = 0;

            for (int b = 0; b < 8; b++) {
                if (children[b].voxelData.signedDistance != 0) {
                    sum += children[b].voxelData.signedDistance;
                    realCount++;
                }
            }

            if (realCount > 0) {
                return sum / realCount;
            } else {
                return 0;
            }
        }

        public bool IdenticalTo(OctNode other) {
            // compare pos, size, type, density and children
            bool childrenIdentical = true;
            if (other.HasChildren != HasChildren) return false;
            if (HasChildren) {
                for (int b = 0; b < 8 && childrenIdentical; b++) {
                    childrenIdentical &= children[b].IdenticalTo(other.children[b]);
                }
            }
            return childrenIdentical &&
            size == other.size &&
            voxelData.blocktype == other.voxelData.blocktype &&
            voxelData.signedDistance == other.voxelData.signedDistance;
        }

        // constants
        private readonly Vector3Int[] cornerOffsets = {
            new Vector3Int(0, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 1, 1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, 1, 1)
        };
    }
}
