using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class MeshBuilder : MonoBehaviour {
        public static MeshBuilder builder;
        
        // Compute stuff
        public ComputeShader shader;
        ComputeBuffer densityBuffer;
        ComputeBuffer typeBuffer;
        ComputeBuffer faceBuffer; 
        ComputeBuffer triCountBuffer;

        int[] faceIndices = new int[] { 0, 4, 3, 4, 7, 3,
                                        3, 7, 2, 7, 6, 2,
                                        1, 5, 0, 5, 4, 0, 
                                        2, 6, 1, 6, 5, 1,
                                        4, 6, 7, 4, 5, 6,
                                        0, 2, 1, 0, 3, 2};

        public void Awake() {
            builder = this;
        }

        void OnDestroy () {
            if (Application.isPlaying) {
                ReleaseBuffers ();
            }
        }

        void CreateBuffers (Vector3Int pointCounts) {
            int numPoints = pointCounts.x * pointCounts.y * pointCounts.z;

            int numVoxels = (pointCounts.x - 1) * (pointCounts.y - 1) * (pointCounts.z - 1);
            int maxFaceCount = numVoxels * 6;

            // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
            // Otherwise, only create if null or if size has changed
            bool bufferSizeChanged = false;
            if (densityBuffer != null) bufferSizeChanged = numPoints != densityBuffer.count;

            if (Application.isPlaying == false || (densityBuffer == null || bufferSizeChanged)) {

                ReleaseBuffers ();
                
                faceBuffer = new ComputeBuffer (maxFaceCount, Face.GetStride(), ComputeBufferType.Append);
                densityBuffer = new ComputeBuffer (numPoints, sizeof (int));
                typeBuffer = new ComputeBuffer (numPoints, sizeof (int));
                triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
            }
        }

        void ReleaseBuffers () {
            if (faceBuffer != null) {
                faceBuffer.Release();
                densityBuffer.Release();
                typeBuffer.Release();
                triCountBuffer.Release();
            }
        }

        public List<Mesh> GenerateMesh(byte[] densityGrid, byte[] typeGrid, Vector3Int resolution, Vector3 offset, out int[] blocktypes) {

            // Setting data inside shader

            //Debug.Log($"Creating buffers of size: {resolution.ToString()}");

            CreateBuffers(resolution);

            int numThreads = Mathf.CeilToInt ((resolution.x) / (float) Globals.threadGroupSize);

            if (typeGrid == null) typeGrid = new byte[resolution.x * resolution.y * resolution.z];

            int[] densityIntGrid = densityGrid.Select(x => (int)x).ToArray();
            int[] typeIntGrid = typeGrid.Select(x => (int)x).ToArray();

            typeBuffer.SetData(typeIntGrid);
            densityBuffer.SetData(densityIntGrid);
            faceBuffer.SetCounterValue (0);

            int kernel = 0;
            shader.SetBuffer(kernel, "pointTypes", typeBuffer);
            shader.SetBuffer (kernel, "density", densityBuffer);
            shader.SetBuffer (kernel, "faces", faceBuffer);

            shader.SetInt ("numPointsX", resolution.x);
            shader.SetInt ("numPointsY", resolution.y);
            shader.SetInt ("numPointsZ", resolution.z);
            shader.SetVector("meshOffset", offset);
            
            shader.Dispatch (kernel, numThreads, numThreads, numThreads);

            // Retrieving data from shader

            ComputeBuffer.CopyCount (faceBuffer, triCountBuffer, 0);
            int[] triCountArray = new int[1];
            triCountBuffer.GetData (triCountArray);
            int numFaces = triCountArray[0];

            Face[] faces = new Face[numFaces];

            faceBuffer.GetData (faces, 0, 0, numFaces);

            return MakeMeshes(faces, resolution, offset, out blocktypes);
        } 

        List<Mesh> MakeMeshes(Face[] faces, Vector3Int resolution, Vector3 offset, out int[] blocktypes) {
            
            // calculate vertex positions
            VoxelVertex[] verticesOfNodes = new VoxelVertex[resolution.x * resolution.y * resolution.z];
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> vertexUVs = new List<Vector2>();

            Dictionary<int, List<Face>> submeshFaces = new Dictionary<int, List<Face>>();
            foreach (Face face in faces) {
                List<Face> value;
                if (!submeshFaces.TryGetValue(face.type, out value)) {
                    value = new List<Face>();
                    submeshFaces.Add(face.type, value);
                }
                value.Add(face);
            }

            blocktypes = submeshFaces.Keys.ToArray();

            Vector3 vertexOffsetSum = Vector3.one * -0.5f + offset;
            float scaleFactor = Mathf.Pow(2, VoxelMesh.LEVEL_OF_DETAIL);
            foreach (int blocktype in blocktypes) {

                foreach (Face meshFace in submeshFaces[blocktype]) {
                    for (int v = 0; v < 4; v++) {
                        Vector3 dcCube = meshFace[v];
                        if (dcCube.x >= 0 && dcCube.x < resolution.x && dcCube.y >= 0 && dcCube.y < resolution.y && dcCube.z >= 0 && dcCube.z < resolution.z) {
                            int i = Globals.LinearIndex((int)dcCube.x, (int)dcCube.y, (int)dcCube.z, resolution);
                            verticesOfNodes[i].AddNeigFace(meshFace);
                            verticesOfNodes[i].isSet = true;
                            verticesOfNodes[i].addedToVertexArray = false;
                        }
                    }
                }

                for (int i = 0; i < verticesOfNodes.Length; i++) { 
                    if (verticesOfNodes[i].isSet && !verticesOfNodes[i].addedToVertexArray) {
                        verticesOfNodes[i].addedToVertexArray = true;
                        verticesOfNodes[i].vertIndex = vertices.Count;
                        vertices.Add((verticesOfNodes[i].ComputePos() + vertexOffsetSum) * scaleFactor);
                        vertexUVs.Add(BlockTypeToUV(verticesOfNodes[i].GetBlockType()));
                    }
                }
            }

            int submeshCount = blocktypes.Length;
            int nextStart = 0;
            List<SubMeshDescriptor> subMeshes = new List<SubMeshDescriptor>();
            
            Mesh mesh = new Mesh();
            mesh.subMeshCount = submeshCount;
            mesh.vertices = vertices.ToArray();
            mesh.uv = vertexUVs.ToArray();
            
            for (int k = 0; k < blocktypes.Length; k++) {
                
                int blocktype = blocktypes[k];
                int submeshStart = nextStart;
                int countIndexes = 0;
                List<int> submeshIndexes = new List<int>();
                int basevertex = 0;

                for (int i = 0; i < submeshFaces[blocktype].Count; i++) {
                    Face faceNow = submeshFaces[blocktype][i];
                    if (!faceNow.IsPartOfMesh(resolution.x)) continue;

                    int[] vertIndices = {
                        Globals.LinearIndex((int)faceNow[0].x, (int)faceNow[0].y, (int)faceNow[0].z, resolution),
                        Globals.LinearIndex((int)faceNow[1].x, (int)faceNow[1].y, (int)faceNow[1].z, resolution),
                        Globals.LinearIndex((int)faceNow[2].x, (int)faceNow[2].y, (int)faceNow[2].z, resolution),
                        Globals.LinearIndex((int)faceNow[3].x, (int)faceNow[3].y, (int)faceNow[3].z, resolution)
                    };
                    
                    // A, B, C, D
                    submeshIndexes.Add(verticesOfNodes[vertIndices[0]].vertIndex);
                    submeshIndexes.Add(verticesOfNodes[vertIndices[1]].vertIndex);
                    submeshIndexes.Add(verticesOfNodes[vertIndices[2]].vertIndex);
                    submeshIndexes.Add(verticesOfNodes[vertIndices[3]].vertIndex);
                    countIndexes += 4;

                    basevertex = Math.Min(verticesOfNodes[vertIndices[0]].vertIndex, basevertex);
                    basevertex = Math.Min(verticesOfNodes[vertIndices[1]].vertIndex, basevertex);
                    basevertex = Math.Min(verticesOfNodes[vertIndices[2]].vertIndex, basevertex);
                    basevertex = Math.Min(verticesOfNodes[vertIndices[3]].vertIndex, basevertex);
                }
                
                mesh.SetIndices(submeshIndexes.ToArray(), MeshTopology.Quads, k, false);
                mesh.SetSubMesh(k, new SubMeshDescriptor(submeshStart, countIndexes, MeshTopology.Quads));
                nextStart += countIndexes;
            }
            
            mesh.RecalculateNormals();

            List<Mesh> meshes = new List<Mesh>();
            meshes.Add(mesh);
            return meshes;
        }

        public void ProcessSimpleBatch(Vector3[] vertices, int[] triangles, Vector3[] normals, Vector2[] uvs, ref int lastFace, int[,,] octrees, Vector3 offset) {
            for (int z = 0; z < 5; z++) {
                for (int y = 0; y < 5; y++) {
                    for (int x = 0; x < 5; x++) {

                        if (octrees[z, y, x] != 0) {
                            Vector3 o = new Vector3(x, y, z) + offset;
                            if (x == 4 || octrees[z, y, x + 1] == 0) {
                                AddFaceToMesh(vertices, triangles, normals, uvs, ref lastFace, Vector3.forward, Vector3.up, o + Vector3.right);
                            }
                            if (x == 0 || octrees[z, y, x - 1] == 0) {
                                AddFaceToMesh(vertices, triangles, normals, uvs, ref lastFace, Vector3.up, Vector3.forward, o);
                            }
                            if (y == 4 || octrees[z, y + 1, x] == 0) {
                                AddFaceToMesh(vertices, triangles, normals, uvs, ref lastFace, Vector3.right, Vector3.forward, o + Vector3.up);
                            }
                            if (y == 0 || octrees[z, y - 1, x] == 0) {
                                AddFaceToMesh(vertices, triangles, normals, uvs, ref lastFace, Vector3.forward, Vector3.right, o);
                            }
                            if (z == 4 || octrees[z + 1, y, x] == 0) {
                                AddFaceToMesh(vertices, triangles, normals, uvs, ref lastFace, Vector3.up, Vector3.right, o + Vector3.forward);
                            }
                            if (z == 0 || octrees[z - 1, y, x] == 0) {
                                AddFaceToMesh(vertices, triangles, normals, uvs, ref lastFace, Vector3.right, Vector3.up, o);
                            }
                        }
                    }
                }
            }
        }

        void AddFaceToMesh(Vector3[] vertices, int[] triangles, Vector3[] normals, Vector2[] uvs, ref int start, Vector3 a, Vector3 b, Vector3 o) {

            vertices[start] = o + b;
            vertices[start + 1] = o + a + b;
            vertices[start + 2] = o + a;
            vertices[start + 3] = o;

            triangles[start] = start;
            triangles[start + 1] = start + 1;
            triangles[start + 2] = start + 2;
            triangles[start + 3] = start + 3;

            Vector3 n = Vector3.Cross(a, b);
            normals[start] = n;
            normals[start + 1] = n;
            normals[start + 2] = n;
            normals[start + 3] = n;

            uvs[start] = new Vector2(vertices[start].x / 125f, vertices[start].z / 125f);
            uvs[start + 1] = new Vector2(vertices[start + 1].x / 125f, vertices[start + 1].z / 125f);
            uvs[start + 2] = new Vector2(vertices[start + 2].x / 125f, vertices[start + 2].z / 125f);
            uvs[start + 3] = new Vector2(vertices[start + 3].x / 125f, vertices[start + 3].z / 125f);

            start += 4;
            if (start > 65532) {
                WrapMeshIntoGameObject(vertices, triangles, normals, uvs, ref start);
            }
        }

        public void WrapMeshIntoGameObject(Vector3[] vertices, int[] indices, Vector3[] normals, Vector2[] uvs, ref int start) {
            
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Quads, 0, false);
            mesh.RecalculateNormals();

            GameObject go = new GameObject("SimpleMapMesh");
            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = Globals.GetSimpleMapMat();

            start = 0;
            Array.Clear(vertices, 0, 65536);
            Array.Clear(indices, 0, 65536);
            Array.Clear(normals, 0, 65536);
            Array.Clear(uvs, 0, 65536);
        }

        Vector2 BlockTypeToUV(int blockType) {
            return new Vector2((blockType % 16)/16f, (blockType / 16)/16f);
        }

        struct Triangle {
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
            public int type;

            public Vector3 this [int i] {
                get {
                    switch (i) {
                        case 0:
                            return a;
                        case 1:
                            return b;
                        default:
                            return c;
                    }
                }
            }
        }

        struct Face : IComparable {
            public Vector3 a, b, c, d;
            public Vector3 surfaceIntersection;
            public int type;
            public Vector3 this [int i] {
                get {
                    switch (i) {
                        case 0:
                            return a;
                        case 1:
                            return b;
                        case 2:
                            return c;
                        default:
                            return d;
                    }
                }
            }

            ///<summary>
            /// Returns stride of one face for Compute shaders
            ///</summary>
            public static int GetStride() {
                return sizeof(float) * 3 * 5 + sizeof(int);
            }

            public bool IsPartOfMesh(int side) {
                return IsVertexPartOfMesh(0, side) && IsVertexPartOfMesh(1, side) && IsVertexPartOfMesh(2, side) && IsVertexPartOfMesh(3, side);
            }
            bool IsVertexPartOfMesh(int i, int side) {
                return this[i].x > 0 && this[i].y > 0 && this[i].z > 0 && this[i].x < side && this[i].y < side && this[i].z < side;
            }
            public Vector3 GetNormal() {
                return Vector3.Cross(this[0] - this[1], this[0] - this[3]).normalized;
            }

            public int CompareTo(object obj)
            {
                if (obj is Face) {
                    if (type == ((Face)obj).type) return 0;
                    return type > ((Face)obj).type ? -1 : 1;
                }
                return -1;
            }
        }

        struct VoxelVertex {
            public List<Face> adjFaces;
            public int vertIndex;
            public bool isSet;
            public bool addedToVertexArray;

            public Vector3 ComputePos() {

                Vector3 res = Vector3.zero;
                int count = 0;

                for (int i = 0; i < adjFaces.Count; ++i) {
                    res += adjFaces[i].surfaceIntersection;
                    count++;
                }

                return res / count;
            }

            public void AddNeigFace(Face _face) {
                if (adjFaces == null) adjFaces = new List<Face>();
                adjFaces.Add(_face);
            } 

            public int GetBlockType() {
                if (adjFaces == null) return 0;
                return adjFaces[0].type;
            }
        }
    }
}