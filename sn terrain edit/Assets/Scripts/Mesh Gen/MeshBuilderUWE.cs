using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Most of this is copied/re-writtten from UWE's VoxelandChunk thing at the moment.

public class MeshBuilderUWE : MonoBehaviour
{
    public static MeshBuilderUWE builder;

    List<VoxelandBlock> blocks;
    List<VoxelandFace> faces;
    List<VoxelandVert> verts;

    void Awake() {
        builder = this;
    }

    public List<Mesh> ComputeMesh(byte[] densityGrid, byte[] typeGrid, int size, Vector3 offset) {
        
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                for (int k = 0; k < size; k++) {
                    byte type = typeGrid[Globals.LinearIndex(i, j, k, size)];
                    byte densityByte = densityGrid[Globals.LinearIndex(i, j, k, size)];
                    if (!OctNodeData.IsBelowSurface(type, densityByte)) {

                    }
                }
            }
        }
        
        return null;
    }

    public Vector3 ComputeSurfaceIntersection(Vector3 p0, Vector3 p1, byte d0, byte d1) {
        Vector3 deltaNormal = (p1 - p0).normalized;
        float d2 = (float)(1 << 5);
        
        if (d0 == 0 && d1 == 0) {
            return (p0 + p1) / 2;
        } else if (d0 == 0) {
            return p1 + ConvertDensity(d1) * deltaNormal / d2;
        } else if (d1 == 0) {
            return p0 + ConvertDensity(d0) * deltaNormal / d1;
        } else {
            if (d0 == d1) {
                return p1;
            }
            float density0 = ConvertDensity(d0), density1 = ConvertDensity(d1);
            float interpolation = -density1 / (density0 - density1);
            return (1 - interpolation) * p1 + interpolation * p0;
        }
    }

    float ConvertDensity(byte b) {
        return (b - 126) / 126f;
    }

    void NewVert() {
        
    }
    void NewFace() {

    }
    void NewBlock() {

    }
}

class VoxelandBlock {

    public int x, y, z;
    public bool visible;    
    public VoxelandFace[] faces;

    public VoxelandBlock() {
        this.faces = new VoxelandFace[6];
    }
}

class VoxelandFace {

    public VoxelandVert[] verts;

    public VoxelandFace() {
        this.verts = new VoxelandVert[9];
    }

    public void LinkVerts(bool skipHiRes) {
        if (skipHiRes)
			{
				this.verts[0].AddNeig(this.verts[2]);
				this.verts[0].AddNeig(this.verts[6]);
				this.verts[2].AddNeig(this.verts[0]);
				this.verts[2].AddNeig(this.verts[4]);
				this.verts[4].AddNeig(this.verts[2]);
				this.verts[4].AddNeig(this.verts[6]);
				this.verts[6].AddNeig(this.verts[0]);
				this.verts[6].AddNeig(this.verts[4]);
				return;
			}
			this.verts[0].AddNeig(this.verts[7]);
			this.verts[0].AddNeig(this.verts[1]);

			this.verts[1].AddNeig(this.verts[0]);
			this.verts[1].AddNeig(this.verts[2]);
			this.verts[1].AddNeig(this.verts[8]);

			this.verts[2].AddNeig(this.verts[1]);
			this.verts[2].AddNeig(this.verts[3]);

			this.verts[3].AddNeig(this.verts[4]);
			this.verts[3].AddNeig(this.verts[2]);
			this.verts[3].AddNeig(this.verts[8]);

			this.verts[4].AddNeig(this.verts[3]);
			this.verts[4].AddNeig(this.verts[5]);

			this.verts[5].AddNeig(this.verts[6]);
			this.verts[5].AddNeig(this.verts[4]);
			this.verts[5].AddNeig(this.verts[8]);
            
			this.verts[6].AddNeig(this.verts[5]);
			this.verts[6].AddNeig(this.verts[7]);

			this.verts[7].AddNeig(this.verts[6]);
			this.verts[7].AddNeig(this.verts[0]);
			this.verts[7].AddNeig(this.verts[8]);

			this.verts[8].AddNeig(this.verts[1]);
			this.verts[8].AddNeig(this.verts[3]);
			this.verts[8].AddNeig(this.verts[5]);
			this.verts[8].AddNeig(this.verts[7]);
    }

    public void WeldVertices(VoxelandFace otherFace, int p1, int p2) {
        if (verts[p1] != null && verts[p1] == otherFace.verts[p2]) return;        
    }
}

class VoxelandVert {

    public byte facePos;
    public bool welded;
    public Vector3 pos;
    public Vector3 relaxed;
    public Vector3 normal;
    public VoxelandVert[] neigs;
    public byte neigCount;
    public VoxelandFace[] adjFaces;
    public float[] blendWeights;

    public VoxelandVert() {
        this.adjFaces = new VoxelandFace[7];
        this.neigs = new VoxelandVert[7];
        this.blendWeights = null;
    }

    public void AddFace(VoxelandFace face) {
        for (int i = 0; i < 7; ++i) {
            if (adjFaces[i] != null && adjFaces[i] == face) {
                return;
            }
            if (adjFaces[i] == null) {
                adjFaces[i] = face;
                return;
            }
        }
    }

    public void AddNeig(VoxelandVert vert) {
        if (neigs.Length > 0) {
            if (neigs.Length != 1) {

                // check if we already have it
                for (int i = 0; i < neigs.Length; ++i) {
                    if (neigs[i] == vert) {
                        return;
                    }
                }
                if ((int)neigCount < neigs.Length) {
                    neigs[(int)neigCount] = vert;
                    neigCount++;
                }
            } else if (neigs[0] != vert) {
                neigs[1] = vert;
                neigCount = 2;
            }
            return;
        }
        neigs[0] = vert;
        neigCount = 1;
    }
}