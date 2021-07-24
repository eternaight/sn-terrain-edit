using UnityEngine;
using ReefEditor.Octrees;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class DebugOctreeDisplay : MonoBehaviour {
        public Vector3Int startOctree;
        public Vector3Int endOctree;
        Octree[,,] octrees;
        [SerializeField] byte[] densityGrid;

        public bool displayOctrees;
        public bool displayDensity;

        void Start() {
            octrees = RegionLoader.loader.GetBatchFromLabel(0).rootNodes;
        }

        void OnValidate() {
            if (octrees != null) {

                for (int k = startOctree.z; k < endOctree.z; k++) {
                    for (int j = startOctree.y; j < endOctree.y; j++) {
                        for (int i = startOctree.x; i < endOctree.x; i++) {

                            densityGrid = new byte[32*32*32];
                            byte[] typeGrid = new byte[32*32*32];
                            octrees[k, j, i].Rasterize(densityGrid, typeGrid, VoxelMesh.RESOLUTION, 4);
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
                                    float w = OctNodeData.DecodeDensity(densityGrid[Globals.LinearIndex(x, y, z, 32)]);
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
}