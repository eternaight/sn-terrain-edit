using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder : MonoBehaviour
{
    public static MeshBuilder builder;

    List<Mesh> meshes;
    
    // Compute stuff
    public ComputeShader shader;
    ComputeBuffer densityBuffer;
    ComputeBuffer typeBuffer;
    ComputeBuffer triBuffer; 
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
        int maxTriangleCount = numVoxels * 6;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        bool bufferSizeChanged = false;
        if (densityBuffer != null) bufferSizeChanged = numPoints != densityBuffer.count;

        if (Application.isPlaying == false || (densityBuffer == null || bufferSizeChanged)) {

            ReleaseBuffers ();
            
            triBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3 + sizeof(int), ComputeBufferType.Append);
            densityBuffer = new ComputeBuffer (numPoints, sizeof (float));
            typeBuffer = new ComputeBuffer (numPoints, sizeof (int));
            triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
        }
    }

    void ReleaseBuffers () {
        if (triBuffer != null) {
            triBuffer.Release ();
            densityBuffer.Release ();
            typeBuffer.Release();
            triCountBuffer.Release ();
        }
    }

    public List<Mesh> MeshFromPoints(float[] density, Vector3Int size, Vector3 offset, int[] pointTypes = null) {

        // Setting data inside shader

        CreateBuffers(size);

        int numThreads = Mathf.CeilToInt ((size.x) / (float) Globals.threadGroupSize);

        densityBuffer.SetData(density);
        triBuffer.SetCounterValue (0);

        if (pointTypes == null) pointTypes = new int[size.x * size.y * size.z];

        typeBuffer.SetData(pointTypes);
        shader.SetBuffer(0, "pointTypes", typeBuffer);

        shader.SetBuffer (0, "density", densityBuffer);
        shader.SetBuffer (0, "triangles", triBuffer);
        
        Debug.Log(numThreads);

        shader.SetInt ("numPointsX", size.x);
        shader.SetInt ("numPointsY", size.y);
        shader.SetInt ("numPointsZ", size.z);
        shader.SetVector("meshOffset", offset);
        
        shader.Dispatch (0, numThreads, numThreads, numThreads);

        // Retrieving data from sahder

        ComputeBuffer.CopyCount (triBuffer, triCountBuffer, 0);
        int[] triCountArray = new int[1];
        triCountBuffer.GetData (triCountArray);
        int numTris = triCountArray[0];

        Triangle[] tris = new Triangle[numTris];

        triBuffer.GetData (tris, 0, 0, numTris);

        // Segmenting geometry into separate meshes (to overcome vertex limit)
        
        List<Mesh> meshes = new List<Mesh>();

        Vector3[] vertices = new Vector3[numTris * 3];
        int[] meshTriangles = new int[numTris * 3];
        Vector2[] meshUVS = new Vector2[numTris * 3];

        while (numTris > 0) {

            int maxTris = 21844;
            int nextMeshTriCount = numTris;
            if (numTris > maxTris) {
                nextMeshTriCount = maxTris;
                numTris -= maxTris;
            }

            for (int i = 0; i < nextMeshTriCount; i++) {

                // one tri
                for (int j = 0; j < 3; j++) {
                    
                    meshTriangles[i * 3 + j] = i * 3 + j;
                    vertices[i * 3 + j] = tris[i + (maxTris * meshes.Count)][j];
                    int type = tris[i + (maxTris * meshes.Count)].type;
                    meshUVS[i * 3 + j] = new Vector2((type % 16)/16f, (type / 16)/16f);
                }
            }
        
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = meshTriangles;
            mesh.uv = meshUVS;

            mesh.RecalculateNormals();

            numTris -= maxTris;
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
}