using ReefEditor.VoxelEditing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor {
    public class MeshBuilderV2 : MonoBehaviour {
        public static MeshBuilderV2 instance;

        private void Awake() {
            instance = this;
        }

        public Mesh GenerateMesh(OctNode node, out int[] blocktypes) {
            var vertexBuffer = new List<Vector3>();
            node.FillVertexList(vertexBuffer);


            var indexBuffer = new List<int>();
            node.UpdateEdgesSolidity();
            CellProc(indexBuffer, node);

            blocktypes = new int[] { 1 };

            var mesh = new Mesh() {
                vertices = vertexBuffer.ToArray(),
                triangles = indexBuffer.ToArray(),
            };

            Debug.Log($"verts: {vertexBuffer.Count}, tris: {indexBuffer.Count / 3}");

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void CellProc(List<int> indexBuffer, OctNode node) {

            if (node.HasChildren) {
                for (int i = 0; i < 8; i++) {
                    CellProc(indexBuffer, node.children[i]);
                }

                for (int i = 0; i < 12; i++) {
                    var faceNodes = new OctNode[2];
                    int[] c = new int[] { cellProcFaceMask[i][0], cellProcFaceMask[i][1] };

                    faceNodes[0] = node.children[c[0]];
                    faceNodes[1] = node.children[c[1]];

                    FaceProc(indexBuffer, faceNodes, cellProcFaceMask[i][2]);
                }

                for (int i = 0; i < 6; i++) {
                    var edgeNodes = new OctNode[4];
                    int[] c = new int[] {
                        cellProcEdgeMask[i][0],
                        cellProcEdgeMask[i][1],
                        cellProcEdgeMask[i][2],
                        cellProcEdgeMask[i][3],
                    };

                    for (int j = 0; j < 4; j++) {
                        edgeNodes[j] = node.children[c[j]];
                    }

                    EdgeProc(indexBuffer, edgeNodes, cellProcEdgeMask[i][4]);
                }
            }
        }

        private static void FaceProc(List<int> indexBuffer, OctNode[] nodes, int dir) {
            if (nodes[0].HasChildren || nodes[1].HasChildren) {
                for (int i = 0; i < 4; i++) {
                    var faceNodes = new OctNode[2];
                    int[] c = {
                        faceProcFaceMask[dir][i][0],
                        faceProcFaceMask[dir][i][1],
                    };

                    for (int j = 0; j < 2; j++) {
                        if (nodes[j].HasChildren) {
                            faceNodes[j] = nodes[j].children[c[j]];
                        } else {
                            faceNodes[j] = nodes[j];
                        }
                    }

                    FaceProc(indexBuffer, faceNodes, faceProcFaceMask[dir][i][2]);
                }

                int[][] orders = new int[][] {
                    new int[] { 0, 0, 1, 1 },
			        new int[] { 0, 1, 0, 1 },
		        };
                for (int i = 0; i < 4; i++) {
                    var edgeNodes = new OctNode[4];
                    int[] c = new int[] {
                        faceProcEdgeMask[dir][i][1],
                        faceProcEdgeMask[dir][i][2],
                        faceProcEdgeMask[dir][i][3],
                        faceProcEdgeMask[dir][i][4],
                    };

                    int[] order = orders[faceProcEdgeMask[dir][i][0]];
                    for (int j = 0; j < 4; j++) {
                        if (nodes[order[j]].HasChildren) {
                            edgeNodes[j] = nodes[order[j]].children[c[j]];
                        } else {
                            edgeNodes[j] = nodes[order[j]];
                        }
                    }

                    EdgeProc(indexBuffer, edgeNodes, faceProcEdgeMask[dir][i][5]);
                }
            }
        }

        private static void EdgeProc(List<int> indexBuffer, OctNode[] nodes, int dir) {
            if (!nodes[0].HasChildren && !nodes[1].HasChildren && !nodes[2].HasChildren && !nodes[3].HasChildren) {
                MakeEdge(indexBuffer, nodes, dir);
            } else {
                OctNode[] edgeNodes = new OctNode[4];
                for (int i = 0; i < 2; i++) {
                    int[] c = new int[] {
                        edgeProcEdgeMask[dir][i][0],
                        edgeProcEdgeMask[dir][i][1],
                        edgeProcEdgeMask[dir][i][2],
                        edgeProcEdgeMask[dir][i][3],
                    };

                    for (int j = 0; j < 4; j++) {
                        if (nodes[j].HasChildren) {
                            edgeNodes[j] = nodes[j].children[c[j]];
                        } else {
                            edgeNodes[j] = nodes[j];
                        }
                    }

                    EdgeProc(indexBuffer, edgeNodes, edgeProcEdgeMask[dir][i][4]);
                }
            }
        }

        private static void MakeEdge(List<int> indexBuffer, OctNode[] nodes, int dir) {
            int minSize = 1000000;  // arbitrary big number
            int minIndex = 0;
            var indices = new int[] { -1, -1, -1, -1 };
            bool flip = false;
            var signChange = new bool[4];

            for (int i = 0; i < 4; i++) {
                int edge = processEdgeMask[dir][i];
                int c1 = edgevmap[edge][0];
                int c2 = edgevmap[edge][1];

                bool solid1 = nodes[i].cornersSolidInfo[c1];
                bool solid2 = nodes[i].cornersSolidInfo[c2];

                if (nodes[i].size < minSize) {
                    minSize = nodes[i].size;
                    minIndex = i;
                    flip = solid1;
                }

                indices[i] = nodes[i].vertexIndex;

                signChange[i] =
                 (solid1 && !solid2) ||
                 (!solid1 && solid2);
            }

            if (signChange[minIndex]) {
                if (!flip) {
                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[1]);
                    indexBuffer.Add(indices[3]);
                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[3]);
                    indexBuffer.Add(indices[2]);
                } else {
                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[3]);
                    indexBuffer.Add(indices[1]);
                    indexBuffer.Add(indices[0]);
                    indexBuffer.Add(indices[2]);
                    indexBuffer.Add(indices[3]);
                }
            }
        }

        private static readonly int[][] processEdgeMask = new int[][] {
            new int[] { 3,2,1,0 }, // right
            new int[] { 7,5,6,4 }, // up
            new int[] { 11,10,9,8 }, // forward
        };
        private static readonly int[][] edgevmap = new int[][] 
        {
	        new int[] {0,4},
            new int[] {1,5},
            new int[] {2,6},
            new int[] {3,7},	// x-axis 
	        new int[] {0,2},
            new int[] {1,3},
            new int[] {4,6},
            new int[] {5,7},	// y-axis
	        new int[] {0,1},
            new int[] {2,3},
            new int[] {4,5},
            new int[] {6,7}		// z-axis
        };


        private static readonly int[][][] edgeProcEdgeMask = new int[][][] {
	        new int[][] {
                new int[] {3,2,1,0,0},
                new int[] {7,6,5,4,0}
            },
            new int[][] {
                new int[] {5,1,4,0,1},
                new int[] {7,3,6,2,1}
            },
            new int[][] {
                new int[] {6,4,2,0,2},
                new int[] {7,5,3,1,2}
            }
        };


        private static readonly int[][][] faceProcFaceMask = new int[][][] {
	        new int[][] {
                new int[] {4,0,0},
                new int[] {5,1,0},
                new int[] {6,2,0},
                new int[] {7,3,0}
            },
            new int[][] {
                new int[] {2,0,1},
                new int[] {6,4,1},
                new int[] {3,1,1},
                new int[] {7,5,1}
            },
            new int[][] {
                new int[] {1,0,2},
                new int[] {3,2,2},
                new int[] {5,4,2},
                new int[] {7,6,2}
            }
        } ;

        private static readonly int[][][] faceProcEdgeMask = new int[][][] {
            new int[][] {
                new int[] {1,4,0,5,1,1},
                new int[] {1,6,2,7,3,1},
                new int[] {0,4,6,0,2,2},
                new int[] {0,5,7,1,3,2}
            },
            new int[][] {
                new int[] {0,2,3,0,1,0},
                new int[] {0,6,7,4,5,0},
                new int[] {1,2,0,6,4,2},
                new int[] {1,3,1,7,5,2}
            },
            new int[][] {
                new int[] {1,1,0,3,2,0},
                new int[] {1,5,4,7,6,0},
                new int[] {0,1,5,0,4,1},
                new int[] {0,3,7,2,6,1}
            }
        };

        private static readonly int[][] cellProcFaceMask = new int[][] 
        {
            new int [] {0,4,0},
            new int [] {1,5,0},
            new int [] {2,6,0},
            new int [] {3,7,0},
            new int [] {0,2,1},
            new int [] {4,6,1},
            new int [] {1,3,1},
            new int [] {5,7,1},
            new int [] {0,1,2},
            new int [] {2,3,2},
            new int [] {4,5,2},
            new int [] {6,7,2}
        };
        private static readonly int[][] cellProcEdgeMask = { 
            new int[] { 0,1,2,3,0},
            new int[] { 4,5,6,7,0},
            new int[] { 0,4,1,5,1},
            new int[] { 2,6,3,7,1},
            new int[] { 0,2,4,6,2},
            new int[] { 1,3,5,7,2} 
        };
    }
}