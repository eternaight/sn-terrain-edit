using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Octree {
    
    public Vector3Int batchIndex;
    OctNode node;
    List<OctNode> leafNodes;
    public int MaxDepth {
        get {
            return node.GetMaxDepth(0);
        }
    }
    public Vector3 Origin {
        get {
            return node.position;
        }
    }

    public Octree(int x, int y, int z, float rootSize, Vector3 batchOrigin) {

        this.batchIndex = new Vector3Int(x, y, z);
        
        node = new OctNode(batchOrigin + new Vector3(x, y, z) * rootSize, rootSize);
        node.rank = NodeRank.Root;

        leafNodes = new List<OctNode>();
    }

    public OctNodeData GetNodeFromPos(Vector3 pos) {
        
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        //sw.Start();
        OctNodeData data = node.GetNodeDataFromPoint(pos);
        //sw.Stop();
        //Debug.Log($"Octree work done in {sw.ElapsedMilliseconds}");

        return data;
    }

    public void Write(OctNodeData[] data) {

        int i = batchIndex.x * 25 + batchIndex.y * 5 + batchIndex.z;
        node.WriteData(data, 0);

        ComputeLeafNodes();
    }

    public OctNodeData[] Read() {
        List<OctNodeData> dataarray = new List<OctNodeData>();

        int i = batchIndex.x * 25 + batchIndex.y * 5 + batchIndex.z;

        dataarray.Add(node.data);
        node.ReadData(ref dataarray, 0);

        return dataarray.ToArray();
    }

    public bool Contains(Vector3 p) {
        return node.ContainsPoint(p);
    }

    void ComputeLeafNodes() {
        leafNodes.AddRange(node.GetLeafs());
    } 

    public void ApplyDensityFunction(SphereDensitySetting density) {
        node.ApplyDensityFunction(density);
    }


    public void Rasterize(byte[] densityGrid, byte[] typeGrid, int side) {
        node.RasterizeTree(densityGrid, typeGrid, side, node.position);
    }
    public OctNodeData[] DeRasterizeGrid(byte[] densityGrid, byte[] typeGrid, int side) {
        node.DeRasterizeGrid(densityGrid, typeGrid, side, node.position);
        return Read();
    }


    public void DrawOctreeGizmos() {
        node.Visualize();
    }


    [System.Serializable]
    private class OctNode {

        public NodeRank rank;

        // data
        public Vector3 position;
        public float size;
        public OctNodeData data;

        // communication fields
        // downward
        public OctNode[] children;

        public OctNode(Vector3 position, float size) {
            rank = NodeRank.Leaf;
            
            this.position = position;
            this.size = size;
        }

        public void Subdivide() {

            if (children == null) {

                children = new OctNode[8];

                for (int b = 0; b < 8; b++) {

                    Vector3 childPosition = position + cornerOffsets[b] * size / 2;

                    children[b] = new OctNode(childPosition, size / 2);
                }

                rank = NodeRank.Branch;
            }
        }
        public void StripChildren() {
            children = null;
            rank = NodeRank.Leaf;
        }

        /// <summary>
        /// writes data into the octree
        /// </summary>
        public void WriteData(OctNodeData[] dataarray, int myPos) {
            
            data = new OctNodeData(dataarray[myPos]);
            
            if (data.childPosition > 0) {

                Subdivide();
                for (int i = 0; i < 8; i++) {
                    children[i].WriteData(dataarray, (data.childPosition + i));
                }

            } else {
                data.childPosition = 0;
            }
        }

        /// <summary>
        /// reads data from the octree
        /// </summary>
        public void ReadData(ref List<OctNodeData> dataarray, int myPos) {

            if (children != null) {
                
                // get new child index
                int newChildIndex = dataarray.Count;
                dataarray[myPos].RewriteChild((ushort)newChildIndex);

                for (int i = 0; i < 8; i++) {
                    dataarray.Add(children[i].data);
                }
                for (int i = 0; i < 8; i++) {
                    children[i].ReadData(ref dataarray, (newChildIndex + i));
                }
            }
        }   

        public OctNodeData GetNodeDataFromPoint(Vector3 p) {
            OctNodeData data = new OctNodeData(false);

            if (ContainsPoint(p)) {

                if (children == null) {
                    // success case
                    return this.data;
                }

                for (int b = 0; b < 8; b++) {
                    OctNodeData childdata = children[b].GetNodeDataFromPoint(p);

                    if (childdata.filled) {
                        // success case follow-up
                        return childdata;
                    }
                }
            }

            // fail case
            return data;
        }

        public bool ContainsPoint(Vector3 p) {
            return OctreeRaycasting.BoxContainsPoint(position, position + Vector3.one * size, p);
        } 

        public OctNode[] GetLeafs() {
            List<OctNode> nodes = new List<OctNode>();

            if (children == null) {
                nodes.Add(this);
            } else {

                for (int i = 0; i < 8; i++) {
                    nodes.AddRange(children[i].GetLeafs());
                }

            }

            return nodes.ToArray();
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

        
        public void RasterizeTree(byte[] densityGrid, byte[] typeGrid, int thisCubeSize, Vector3 octreeOrigin) {

            if (children != null) {
                for (int b = 0; b < 8; b++) {
                    children[b].RasterizeTree(densityGrid, typeGrid, thisCubeSize / 2, octreeOrigin);
                }
            } else {
                
                float thisDensity = data.GetDensity();
                Vector3 localPos = this.position - octreeOrigin;
                Vector3Int start = new Vector3Int((int)localPos.x, (int)localPos.y, (int)localPos.z);

                for (int k = start.z; k < start.z + thisCubeSize; ++k) {
                    for (int j = start.y; j < start.y + thisCubeSize; ++j) {
                        for (int i = start.x; i < start.x + thisCubeSize; ++i) {
                            typeGrid[Globals.LinearIndex(i, j, k, 32)] = data.type;
                            densityGrid[Globals.LinearIndex(i, j, k, 32)] = data.signedDist;
                        }
                    }
                }
            }
        }

        public void DeRasterizeGrid(byte[] densityGrid, byte[] typeGrid, int gridSide, Vector3 octreeOrigin) {
            if (this.size > 1) {
                Subdivide();

                for (int b = 0; b < 8; b++) {
                    children[b].DeRasterizeGrid(densityGrid, typeGrid, gridSide, octreeOrigin);
                }

                if (IsMonotone()) StripChildren();
            }
            else {
                Vector3 localPos = this.position - octreeOrigin;
                int gridIndex = Globals.LinearIndex((int)localPos.x, (int)localPos.y, (int)localPos.z, gridSide);

                this.data.type = typeGrid[gridIndex];
                this.data.signedDist = densityGrid[gridIndex];
            }
        }

        public void ApplyDensityFunction(SphereDensitySetting density) {
            float sampleDensity = density.SphereDensity(position + Vector3.one * size * 0.5f);
            float densityNow = Mathf.Max(sampleDensity, data.GetDensity());

            data.SetDensity(densityNow);
            if (densityNow >= 0) data.type = Brush.selectedType;
            else data.type = 0;

            if (children == null) {

                // subdivide if brush is inside node
                float dist = OctreeRaycasting.DistanceToBox(density.origin, position, position + Vector3.one * size);
                bool brushInsideNode = dist <= density.radius;

                if (brushInsideNode && size > 1) {
                    Subdivide();
                }
            }

            if (children != null) {
                for (int b = 0; b < 8; b++) {
                    children[b].ApplyDensityFunction(density);
                }
            }

            //if (IsMonotone()) StripChildren();
        }

        bool IsMonotone() {

            if (children == null) return true;

            int childDensity = children[0].data.signedDist;
            for (int b = 1; b < 8; b++) {
                if (childDensity != children[b].data.signedDist) {
                    return false;
                }
            }

            return true;
        }

        public void Visualize() {

            if (children != null) {
                foreach (OctNode node in children) {
                    node.Visualize();
                }
            } else {
                float density = data.signedDist;//data.GetDensity();
                // if (density > 0) {
                //     float v = density / 126f;
                //     Gizmos.color = new Color(v, v, v);
                //     Gizmos.DrawCube(position + Vector3.one * size * 0.5f, Vector3.one * size);
                // }
                if (density == 0) {
                    bool solid = data.type != 0;
                    Gizmos.color = solid ? Color.yellow : Color.white;
                    Gizmos.DrawCube(position + Vector3.one * size * 0.5f, Vector3.one * size);
                }
            }
        } 
 
        // constants
        static Vector3[] cornerOffsets = {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1)
        };
    }
}

public enum NodeRank {
    Root,
    Branch,
    Leaf
} 

public struct OctNodeData {
    
    public bool filled;
    public byte type;
    public byte signedDist;
    public ushort childPosition;

    public OctNodeData(bool filled) {
        this.filled = false;
        this.type = 0;
        this.signedDist = 0;
        this.childPosition = 0;
    }
    public OctNodeData(byte type, byte signedDistance, ushort childPos) {
        this.filled = true;
        this.type = type;
        this.signedDist = signedDistance;
        this.childPosition = childPos;
    }
    public OctNodeData(OctNodeData other) {
        this.filled = other.filled;
        this.type = other.type;
        this.signedDist = other.signedDist;
        this.childPosition = other.childPosition;
    }
    public void RewriteChild(ushort newIndex) {
        this.childPosition = newIndex;
    }

    public bool IsBelowSurface() {
        if (signedDist == 0) 
        {
            return type > 0;
        }
        return signedDist >= 126;
    }
    public static bool IsBelowSurface(byte type, byte signedDist) {
        if (signedDist == 0) 
        {
            return type > 0;
        }
        return signedDist >= 126;
    }

    public float GetDensity() {

        if (filled) {
            if (signedDist == 0) 
            {
                return (type == 0 ? -1 : 1);
            }
            return (signedDist - 126) / 126f;
        }
        return -1f;
    }
    public void SetDensity(float val) {
        if (val == 1 || val == -1) {
            signedDist = 0;
        }
        signedDist = (byte)(val * 126 + 126);
    }
    public static float DecodeDensity(byte densityByte) {
        return (densityByte - 126) / 126f;
    }

    public override string ToString() {
        return $"t: {type}, d: {signedDist}, c: {childPosition}";
    }

    public override bool Equals(object obj) {
        if (obj is OctNodeData) {
            return(
                ((OctNodeData)obj).filled == filled &&
                ((OctNodeData)obj).type == type &&
                ((OctNodeData)obj).signedDist == signedDist &&
                ((OctNodeData)obj).childPosition == childPosition
            ); 
        }
        return false;
    }
    public override int GetHashCode() {
        return type ^ signedDist ^ childPosition;
    }

    public static bool operator ==(OctNodeData one, OctNodeData other) {
        return (
            other.filled == one.filled &&
            other.type == one.type &&
            other.signedDist == one.signedDist &&
            other.childPosition == one.childPosition
        );
    }
    public static bool operator !=(OctNodeData one, OctNodeData other) {
        return !(
            other.filled == one.filled &&
            other.type == one.type &&
            other.signedDist == one.signedDist &&
            other.childPosition == one.childPosition
        );
    }
}

public struct DensityCube {
    public Vector3Int start;
    public int size;
    public float densityValue;
} 