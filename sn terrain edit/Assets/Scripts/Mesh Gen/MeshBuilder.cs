using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshBuilder : MonoBehaviour
{
    public static MeshBuilder builder;

    List<Mesh> meshes;
    
    // Compute stuff
    public ComputeShader shader;
    ComputeBuffer densityBuffer;
    ComputeBuffer typeBuffer;
    ComputeBuffer faceBuffer; 
    ComputeBuffer triCountBuffer;

    Batch batch;

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
        int maxFaceCount = numVoxels;

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

    public List<Mesh> GenerateMesh(byte[] densityGrid, byte[] typeGrid, Vector3Int size, Vector3 offset) {

        // Setting data inside shader

        //Debug.Log($"Creating buffers of size: {size.ToString()}");

        CreateBuffers(size);

        int numThreads = Mathf.CeilToInt ((size.x) / (float) Globals.threadGroupSize);

        if (typeGrid == null) typeGrid = new byte[size.x * size.y * size.z];

    	int[] densityIntGrid = densityGrid.Select(x => (int)x).ToArray();
    	int[] typeIntGrid = typeGrid.Select(x => (int)x).ToArray();

        typeBuffer.SetData(typeIntGrid);
        densityBuffer.SetData(densityIntGrid);
        faceBuffer.SetCounterValue (0);

        int kernel = 0;
        shader.SetBuffer(kernel, "pointTypes", typeBuffer);
        shader.SetBuffer (kernel, "density", densityBuffer);
        shader.SetBuffer (kernel, "faces", faceBuffer);

        shader.SetInt ("numPointsX", size.x);
        shader.SetInt ("numPointsY", size.y);
        shader.SetInt ("numPointsZ", size.z);
        shader.SetVector("meshOffset", offset);
        
        shader.Dispatch (kernel, numThreads, numThreads, numThreads);

        // Retrieving data from sahder

        ComputeBuffer.CopyCount (faceBuffer, triCountBuffer, 0);
        int[] triCountArray = new int[1];
        triCountBuffer.GetData (triCountArray);
        int numFaces = triCountArray[0];

        Face[] faces = new Face[numFaces];

        faceBuffer.GetData (faces, 0, 0, numFaces);

        // calculate vertex positions
        VoxelVertex[] verticesOfNodes = new VoxelVertex[size.x * size.y * size.z];
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> vertexUVs = new List<Vector2>();

        foreach (Face meshFace in faces) {
            for (int v = 0; v < 4; v++) {
                Vector3 dcCube = meshFace[v];
                if (dcCube.x >= 0 && dcCube.x < 34 && dcCube.y >= 0 && dcCube.y < 34 && dcCube.z >= 0 && dcCube.z < 34) {
                    int i = Globals.LinearIndex((int)dcCube.x, (int)dcCube.y, (int)dcCube.z, size);
                    verticesOfNodes[i].AddNeigFace(meshFace);
                    verticesOfNodes[i].isSet = true;
                }
            }
        }

        Vector3 vertexOffsetSum = Vector3.one * -0.5f + offset;
        for (int i = 0; i < verticesOfNodes.Length; i++) { 
            if (verticesOfNodes[i].isSet) {
                verticesOfNodes[i].vertIndex = vertices.Count;
                vertices.Add(verticesOfNodes[i].ComputePos() + vertexOffsetSum);
                vertexUVs.Add(BlockTypeToUV(verticesOfNodes[i].GetBlockType()));
            }
        }


        // Segmenting geometry into separate meshes (to overcome vertex limit)
        List<Mesh> meshes = new List<Mesh>();
        List<int> meshTriangles = new List<int>();

        int maxFaces = 10922;
        int facesRemaining = numFaces;

        while (facesRemaining > 0) {

            int nextFacePool = Math.Min(facesRemaining, maxFaces);

            for (int i = 0; i < nextFacePool; i++) {
                Face faceNow = faces[i + maxFaces * meshes.Count];
                if (!faceNow.IsPartOfMesh()) continue;

                int[] vertIndices = {
                    Globals.LinearIndex((int)faceNow[0].x, (int)faceNow[0].y, (int)faceNow[0].z, size),
                    Globals.LinearIndex((int)faceNow[1].x, (int)faceNow[1].y, (int)faceNow[1].z, size),
                    Globals.LinearIndex((int)faceNow[2].x, (int)faceNow[2].y, (int)faceNow[2].z, size),
                    Globals.LinearIndex((int)faceNow[3].x, (int)faceNow[3].y, (int)faceNow[3].z, size)
                };
                
                // A, B, C, A, C, D
                meshTriangles.Add(verticesOfNodes[vertIndices[0]].vertIndex);
                meshTriangles.Add(verticesOfNodes[vertIndices[1]].vertIndex);
                meshTriangles.Add(verticesOfNodes[vertIndices[2]].vertIndex);
                meshTriangles.Add(verticesOfNodes[vertIndices[0]].vertIndex);
                meshTriangles.Add(verticesOfNodes[vertIndices[2]].vertIndex);
                meshTriangles.Add(verticesOfNodes[vertIndices[3]].vertIndex);
            }
        
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = meshTriangles.ToArray();
            mesh.uv = vertexUVs.ToArray();
            
            mesh.RecalculateNormals();

            facesRemaining -= nextFacePool;
            meshes.Add(mesh);
        }
        return meshes;
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

    struct Face {
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

        public bool IsPartOfMesh() {
            return IsVertexPartOfMesh(0) && IsVertexPartOfMesh(1) && IsVertexPartOfMesh(2) && IsVertexPartOfMesh(3);
        }
        bool IsVertexPartOfMesh(int i) {
            return this[i].x > 0 && this[i].y > 0 && this[i].z > 0 && this[i].x < 34 && this[i].y < 34 && this[i].z < 34;
        }
        public Vector3 GetNormal() {
            return Vector3.Cross(this[0] - this[1], this[0] - this[3]).normalized;
        }
    }

    struct VoxelVertex {
        public List<Face> adjFaces;
        public int vertIndex;
        public bool isSet;

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