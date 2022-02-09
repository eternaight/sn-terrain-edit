using System;
using System.Collections.Generic;
using UnityEngine;
using ReefEditor.Streaming;

namespace ReefEditor.VoxelEditing {

    [Serializable]
    public class OctNode {

        // data
        public Vector3Int position;
        public int size;
        public OctNodeData data;

        public bool HasChildren { get; private set; }
        public OctNode[] children;

        public OctNode(Vector3Int position, int size) {

            HasChildren = false;
            this.position = position;
            this.size = size;
            data = new OctNodeData();
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
                }

                HasChildren = true;
            }
        }
        public void StripChildren() {
            children = null;
            HasChildren = false;
        }

        /// <summary>
        /// writes data into the octree
        /// </summary>
        public void ReadArray(OctNodeData[] dataarray, int myPos) {

            data = new OctNodeData(dataarray[myPos]);

            if (data.childPosition > 0) {
                Subdivide();
                for (int i = 0; i < 8; i++) {
                    children[i].ReadArray(dataarray, data.childPosition + i);
                }
                HasChildren = true;
            }
        }

        /// <summary>
        /// reads data from the octree
        /// </summary>
        public void WriteToArray(List<OctNodeData> dataArray) {
            dataArray.Add(data);
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
                    OctNodeData childData = new OctNodeData(children[i].data.type, children[i].data.density, 0);
                    dataarray.Add(childData);
                }
                for (int i = 0; i < 8; i++) {
                    children[i].WriteChildrenToArray(dataarray, (newChildIndex + i));
                }
            }
        }

        public int GetMaxDepth(int prevDepth) {
            if (children == null) return prevDepth + 1;
            else {
                return Mathf.Max(
                    children[0].GetMaxDepth(prevDepth + 1),
                    children[1].GetMaxDepth(prevDepth + 1),
                    children[2].GetMaxDepth(prevDepth + 1),
                    children[3].GetMaxDepth(prevDepth + 1),
                    children[4].GetMaxDepth(prevDepth + 1),
                    children[5].GetMaxDepth(prevDepth + 1),
                    children[6].GetMaxDepth(prevDepth + 1),
                    children[7].GetMaxDepth(prevDepth + 1)
                );
            }
        }


        // About octree heights: 0 = rootx32, 1x16, 2x8, 3x4, 4x2, 5x1. Total of 6 levels.
        public OctNodeData GetVoxel(Vector3Int voxel, int maxSearchHeight, int height = 0) {
            if (HasChildren && (height < maxSearchHeight)) {
                for (int i = 0; i < 8; i++) {
                    if (children[i].ContainsVoxel(voxel)) {
                        return children[i].GetVoxel(voxel, maxSearchHeight, height + 1);
                    }
                }
                return null;
            } else {
                return data;
            }
        }
        public bool ContainsVoxel(Vector3Int voxel) {
            return (
                voxel.x >= position.x && voxel.x < position.x + size &&
                voxel.y >= position.y && voxel.y < position.y + size &&
                voxel.z >= position.z && voxel.z < position.z + size
            );
        }

        public void DeRasterizeGrid(VoxelGrid grid, int maxHeight, int height = 0) {
            if (height < maxHeight) {
                Subdivide();

                for (int b = 0; b < 8; b++) {
                    children[b].DeRasterizeGrid(grid, maxHeight, height + 1);
                }

                data = new OctNodeData(MostCommonChildType(), AverageChildDensity(), 0);

                if (IsMonotone()) StripChildren();
            } else {
                data = grid.GetVoxel(position);
            }
        }

        bool IsMonotone() {

            if (!HasChildren) return true;

            OctNodeData t1 = children[0].data;
            int childDensity = t1.density;
            for (int b = 1; b < 8; b++) {
                if (childDensity != children[b].data.density) {
                    return false;
                }
            }

            return true;
        }

        public void Visualize() {

            if (HasChildren) {
                foreach (OctNode node in children) {
                    node.Visualize();
                }
            } else {
                if (data.density == 0) {
                    bool solid = data.type != 0;
                    Gizmos.color = solid ? Color.yellow : Color.white;
                    Gizmos.DrawCube(position + Vector3.one * size * 0.5f, Vector3.one * size);
                }
            }
        }

        byte MostCommonChildType() {
            if (!HasChildren) return data.type;

            for (int b = 0; b < 8; b++) {
                if (children[b].data.type != 0) {
                    return children[b].data.type;
                }
            }

            return 0;
        }
        byte AverageChildDensity() {
            if (!HasChildren) return data.density;
            int sum = 0;
            float realCount = 0;

            for (int b = 0; b < 8; b++) {
                if (children[b].data.density != 0) {
                    sum += children[b].data.density;
                    realCount++;
                }
            }

            if (realCount > 0) {
                return (byte)(sum / realCount);
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
            data.type == other.data.type &&
            data.density == other.data.density;
        }

        // constants
        static Vector3Int[] cornerOffsets = {
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
