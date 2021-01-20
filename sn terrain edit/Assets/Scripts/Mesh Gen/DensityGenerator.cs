using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityGenerator : MonoBehaviour
{
    public static DensityGenerator density;
    RegionLoader region;
    
    public delegate float DensityFunction(Vector3 v);
    
    public void Awake() {
        density = this;
    }
    public void Start() {
        region = RegionLoader.loader;
    }

    public Vector4[] GeneratePointField(Vector3Int pointCount, float cubeSize, Vector3 offset) {
        
        int count = pointCount.x * pointCount.y * pointCount.z;
    
        Vector4[] points = new Vector4[count];

        for (int z = 0; z < pointCount.z; z++) {
            for (int y = 0; y < pointCount.y; y++) {
                for (int x = 0; x < pointCount.x; x++) {

                    Vector3 vertex = new Vector3(x, y, z) * cubeSize + offset;
                    points[indexFromId(x, y, z, pointCount)] = new Vector4(vertex.x, vertex.y, vertex.z, 0);
                }
            }
        }

        return points;
    }

    public void ApplyBatchDensity(ref Vector4[] originalPoints, Vector3Int pointCount, Vector3 sampleOrigin, ref int[] pointTypes) {

        for (int z = 0; z < pointCount.z; z++) {
            for (int y = 0; y < pointCount.y; y++) {
                for (int x = 0; x < pointCount.x; x++) {

                    int i = indexFromId(x, y, z, pointCount);
                    Vector3 vertex = (Vector3)(originalPoints[i]) + sampleOrigin;

                    PointData point = BatchDensityEnhanced(vertex);
                    originalPoints[i].w += point.density;
                    pointTypes[i] = point.type;
                }
            }
        }
    }

    public void ApplySphereDensity(ref Vector4[] originalPoints, Vector3Int pointCount, Vector3 sphereOrigin, float sphereRadius) {

        for (int z = 0; z < pointCount.z; z++) {
            for (int y = 0; y < pointCount.y; y++) {
                for (int x = 0; x < pointCount.x; x++) {
                    
                    int i = indexFromId(x, y, z, pointCount);
                    Vector3 vertex = originalPoints[i];
                    float d = Mathf.Max(originalPoints[i].w, SphereDensity(vertex, sphereOrigin, sphereRadius));

                    originalPoints[i].w = d;
                }
            }
        }
    }

    public void ApplyEdgeDensity(float[] originalPoints, int[] pointTypes, Vector3Int pointCount, Vector3Int containerIndex) {

        bool isOnOuterEdge = (containerIndex.x == 4 || containerIndex.y == 4 || containerIndex.z == 4);
        bool isOnInnerEdge = (containerIndex.x == 0 || containerIndex.y == 0 || containerIndex.z == 0);

        if (!isOnInnerEdge && !isOnOuterEdge) {
            return;
        }

        
        for (int z = 0; z < pointCount.z; z++) {
            for (int y = 0; y < pointCount.y; y++) {
                for (int x = 0; x < pointCount.x; x++) {

                    bool resetPoint = false;

                    if (x == 0 && containerIndex.x == 0) resetPoint = true;
                    if (y == 0 && containerIndex.y == 0) resetPoint = true;
                    if (z == 0 && containerIndex.z == 0) resetPoint = true; 
                    if (x == pointCount.x - 2 && containerIndex.x == 4) resetPoint = true;
                    if (y == pointCount.y - 2 && containerIndex.y == 4) resetPoint = true;
                    if (z == pointCount.z - 2 && containerIndex.z == 4) resetPoint = true; 

                    if (resetPoint) {
                        int index = indexFromId(x, y, z, pointCount);
                        originalPoints[index] = -1;
                        pointTypes[index] = 0;
                    }
                }
            }
        }

    }

    int indexFromId(int x, int y, int z, Vector3Int size) {
        return z * size.y * size.x + y * size.x + x;
    }

    public float SphereDensity(Vector3 point, Vector3 sphereOrigin, float radius) {
        return radius - (point - sphereOrigin).magnitude;
    }
    public PointData BatchDensityEnhanced(Vector3 point) {
        int x = (int)(point.x / 160);
        int y = (int)(point.y / 160);
        int z = (int)(point.z / 160);
        
        Batch batch = region.GetBatchLocal(x, y, z);
        if (batch) {
            return new PointData(batch.GetNode(point));
        }

        PointData exteriorData = new PointData(0, -1);
        return exteriorData;
    }

    Vector3Int GetOctreeIndex(int x, int y, int z, Vector3Int octI) {

        return new Vector3Int(
            ((x == -1 && octI.x > 0) ? -1 : 0),
            ((y == -1 && octI.y > 0) ? -1 : 0),
            ((z == -1 && octI.z > 0) ? -1 : 0));
    }
}

public struct PointData {
    public int type;
    public float density;
    public PointData(int t, float d) {
        type = t;
        density = d;
    }
    public PointData(OctNodeData data) {
        type = data.type;
        density = data.GetDensity();
    }
}

public class SphereDensitySetting {
    public Vector3 origin;
    public float radius;
    public float SphereDensity(Vector3 sample) {
        return Mathf.Clamp(radius - (sample - origin).magnitude, -1, 1);
    }
}