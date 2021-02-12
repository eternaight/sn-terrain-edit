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
    float time;

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

    public List<Mesh> MeshFromPoints(byte[] densityGrid, byte[] typeGrid, Vector3Int size, Vector3 offset) {

        // Setting data inside shader

        Debug.Log($"Creating buffers of size: {size.ToString()}");

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

        foreach (Face meshFace in faces) {
            for (int v = 0; v < 4; v++) {
                Vector3 dcCube = meshFace[v];
                if (dcCube.x >= 0 && dcCube.x < 34 && dcCube.y >= 0 && dcCube.y < 34 && dcCube.z >= 0 && dcCube.z < 34)
                    verticesOfNodes[Globals.LinearIndex((int)dcCube.x, (int)dcCube.y, (int)dcCube.z, size)].AddNeigFace(meshFace);
            }
        }

        // Segmenting geometry into separate meshes (to overcome vertex limit)
        
        List<Mesh> meshes = new List<Mesh>();

        Vector3[] vertices = new Vector3[numFaces * 4];
        List<int> meshTriangles = new List<int>();
        Vector2[] meshUVS = new Vector2[numFaces * 4];

        int maxFaces = 10922;
        int facesRemaining = numFaces;

        Vector3 vertexOffsetSum = Vector3.one * -0.5f + offset;

        while (facesRemaining > 0) {

            int nextFacePool = Math.Min(facesRemaining, maxFaces);

            for (int i = 0; i < nextFacePool; i++) {

                // for each face:
                int faceIndex = i + maxFaces * meshes.Count;

                Face faceNow = faces[faceIndex];
                if (!faceNow.IsPartOfMesh()) continue;
                
                for (int v = 0; v < 4; v++) {
                    //vertices[i * 4 + v] = faceNow[v] + vertexOffsetSum;
                    vertices[i * 4 + v] = verticesOfNodes[Globals.LinearIndex((int)faceNow[v].x, (int)faceNow[v].y, (int)faceNow[v].z, size)].ComputePos() + vertexOffsetSum;
                }
                
                // A, B, C, A, C, D
                meshTriangles.Add(i * 4);
                meshTriangles.Add(i * 4 + 1);
                meshTriangles.Add(i * 4 + 2);
                meshTriangles.Add(i * 4);
                meshTriangles.Add(i * 4 + 2);
                meshTriangles.Add(i * 4 + 3);

                int faceType = faceNow.type;
                meshUVS[i * 4] =     new Vector2((faceType % 16)/16f, (faceType / 16)/16f);
                meshUVS[i * 4 + 1] = new Vector2((faceType % 16)/16f, (faceType / 16)/16f);
                meshUVS[i * 4 + 2] = new Vector2((faceType % 16)/16f, (faceType / 16)/16f);
                meshUVS[i * 4 + 3] = new Vector2((faceType % 16)/16f, (faceType / 16)/16f);
            }
        
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = meshTriangles.ToArray();
            mesh.uv = meshUVS;

            mesh.RecalculateNormals();

            facesRemaining -= nextFacePool;
            meshes.Add(mesh);
        }
        return meshes;
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
        public int dir;
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
            return sizeof(float) * 3 * 5 + sizeof(int) * 2;
        }

        public bool IsPartOfMesh() {
            return IsVertexPartOfMesh(0) && IsVertexPartOfMesh(1) && IsVertexPartOfMesh(2) && IsVertexPartOfMesh(3);
        }
        bool IsVertexPartOfMesh(int i) {
            return this[i].x > 0 && this[i].y > 0 && this[i].z > 0 && this[i].x < 34 && this[i].y < 34 && this[i].z < 34;
        }
    }

    struct VoxelVertex {
        List<Face> adjFaces;

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
    }
}