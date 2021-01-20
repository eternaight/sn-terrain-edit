using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugOctreeDisplay : MonoBehaviour
{
    public Vector3Int startOctree;
    public Vector3Int endOctree;
    Octree[,,] octrees;
    float[][] density;

    public bool displayOctrees;
    public bool displayDensity;

    void Start() {
        octrees = RegionLoader.loader.GetBatchFromLabel(0).rootNodes;
    }

    void OnValidate() {
        if (octrees != null) {

            Vector3Int size = endOctree - startOctree + Vector3Int.one;
            int len = size.x * size.y * size.z;
            density = new float[len][];

            for (int k = startOctree.z; k < endOctree.z; k++) {
                for (int j = startOctree.y; j < endOctree.y; j++) {
                    for (int i = startOctree.x; i < endOctree.x; i++) {

                        Vector3Int index = new Vector3Int(i, j, k) - startOctree;
                        int densityNow = index.z * size.y * size.x + index.y * size.x + index.x;
                        density[densityNow] = new float[32*32*32];
                        Queue<DensityCube> cubeQueue = octrees[k, j, i].FillDensityArray(32);

                        foreach (DensityCube dube in cubeQueue) {
                            for (int z = dube.start.z; z < dube.size; z++) {
                                for (int y = dube.start.y; y < dube.size; y++) {
                                    for (int x = dube.start.x; x < dube.size; x++) {
                                        density[densityNow][(x + y * 32 + z * 1024)] = dube.densityValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
    }

    void OnDrawGizmos() {
        if (octrees != null) {

            if (displayOctrees) {                

                octrees[startOctree.z, startOctree.y, startOctree.x].DrawOctreeGizmos();
                octrees[endOctree.z, endOctree.y, endOctree.x].DrawOctreeGizmos();
            
            } else if (displayDensity) {
                DrawDensityGizmos();
            }
        }
    }

    void DrawDensityGizmos() {

        Vector3Int size = endOctree - startOctree + Vector3Int.one;
        
        for (int k = startOctree.z; k < endOctree.z; k++) {
            for (int j = startOctree.y; j < endOctree.y; j++) {
                for (int i = startOctree.x; i < endOctree.x; i++) {
                    
                    Vector3Int index = new Vector3Int(i, j, k) - startOctree;
                    int densityNow = index.z * size.y * size.x + index.y * size.x + index.x;

                    for (int z = 0; z < 32; z++) {
                        for (int y = 0; y < 32; y++) {
                            for (int x = 0; x < 32; x++) {
                                float w = density[densityNow][pindex(x, y, z, 32)];
                                Vector3 pos = new Vector3(x, y, z) + octrees[k, j, i].Origin;
                                Gizmos.color = new Color(w, w, w);
                                Gizmos.DrawSphere(pos, .25f);
                            }
                        }
                    }
                }
            }
        }
    }

    int pindex(int x, int y, int z, int side) {
        return x + y * side + z * side * side;
    }
}
