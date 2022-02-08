using System;
using System.Collections.Generic;
using UnityEngine;
using ReefEditor.VoxelTech;

namespace ReefEditor.Octrees {

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

        public OctNodeData GetNodeDataFromPoint(Vector3 p) {
            if (ContainsPoint(p)) {

                if (children == null) {
                    // success case
                    return data;
                }

                for (int b = 0; b < 8; b++) {
                    OctNodeData childdata = children[b].GetNodeDataFromPoint(p);

                    if (!(childdata is null)) {
                        // success case follow-up
                        return childdata;
                    }
                }
            }

            // fail case
            return null;
        }

        public bool ContainsPoint(Vector3 p) {
            return OctreeRaycasting.BoxContainsPoint(position, position + Vector3.one * size, p);
        }
        public bool ContainsVoxel(Vector3Int voxel) {
            return (
                voxel.x >= position.x && voxel.x < position.x + size &&
                voxel.y >= position.y && voxel.y < position.y + size &&
                voxel.z >= position.z && voxel.z < position.z + size
                );
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


        public OctNodeData GetVoxel(Vector3Int voxel) {
            if (HasChildren) {
                for (int i = 0; i < 8; i++) {
                    if (children[i].ContainsVoxel(voxel)) {
                        return children[i].GetVoxel(voxel);
                    }
                }
                return null;
            } else {
                return data;
            }
        }

        public void DeRasterizeGrid(VoxelGrid grid, int height, int maxHeight) {
            if (height < maxHeight) {
                Subdivide();

                for (int b = 0; b < 8; b++) {
                    children[b].DeRasterizeGrid(grid, height + 1, maxHeight);
                }

                data = new OctNodeData(MostCommonChildType(), AverageChildDensity(), 0);

                if (IsMonotone()) StripChildren();
            } else {
                byte[] voxel = grid.GetVoxel(position);
                data = new OctNodeData(voxel[0], voxel[1], 0);
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

    public class OctNodeData {

        public byte type;
        public byte density;
        public ushort childPosition;

        public OctNodeData() {
            type = 0;
            density = 0;
            childPosition = 0;
        }
        public OctNodeData(byte type, byte signedDistance, ushort childPos) {
            this.type = type;
            density = signedDistance;
            childPosition = childPos;
        }
        public OctNodeData(OctNodeData other) {
            type = other.type;
            density = other.density;
            childPosition = other.childPosition;
        }

        public bool IsBelowSurface() {
            if (density == 0) {
                return type > 0;
            }
            return density >= 126;
        }
        public static bool IsBelowSurface(byte type, byte signedDist) {
            if (signedDist == 0) {
                return type > 0;
            }
            return signedDist >= 126;
        }

        public float GetDensity() {
            if (density == 0) {
                return (type == 0 ? -1 : 1);
            }
            return (density - 126) / 126f;
        }
        public static float DecodeDensity(byte densityByte) {
            return (densityByte - 126) / 126f;
        }
        public static byte EncodeDensity(float densityValue) {
            return (byte)(Mathf.Clamp(densityValue, -1, 1) * 126 + 126);
        }

        public override string ToString() {
            return $"OctNode(t: {type}, d: {density}, c: {childPosition})";
        }

        public override bool Equals(object obj) {
            if (obj is OctNodeData data) {
                return (
                    data.type == type &&
                    data.density == density &&
                    data.childPosition == childPosition
                );
            }
            return false;
        }

        public override int GetHashCode() {
            return (childPosition * 31 + type) * 31 + density;
        }

        public static bool operator ==(OctNodeData one, OctNodeData other) {
            return (
                other.type == one.type &&
                other.density == one.density &&
                other.childPosition == one.childPosition
            );
        }
        public static bool operator !=(OctNodeData one, OctNodeData other) {
            return (
                other.type != one.type ||
                other.density != one.density ||
                other.childPosition != one.childPosition
            );
        }
    }
}
