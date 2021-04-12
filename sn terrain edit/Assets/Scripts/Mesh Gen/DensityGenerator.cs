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
    public static VoxelMesh.VoxelGrid GenerateMaterialGallery(Vector3Int octreeIndex, int pointCount = 32, int startMatType = 0) {
        
        int count = pointCount * pointCount * pointCount;

        byte[,,] densityPoints = new byte[pointCount, pointCount, pointCount];
        byte[,,] typePoints = new byte[pointCount, pointCount, pointCount];

        startMatType = 0;
        if (octreeIndex.x == 2 && octreeIndex.z == 2 && octreeIndex.y == 3) startMatType = 97;
        if (octreeIndex.x == 1 && octreeIndex.z == 2 && octreeIndex.y == 3) startMatType = 105;

        if (startMatType != 0) {

            int side = 30;
            int index = startMatType;
            int end = startMatType + 8;

            for (int i = 1; i < side; i += 4) {
                for (int j = 1; j < side; j += 4) {

                    typePoints[i, 16, j] = 1;
                    for (int x = -1; x < 2; x++) {
                        for (int y = -1; y < 2; y++) {
                            for (int z = -1; z < 2; z++) {
                                if (x == 0 && y == 0 && z == 0) {
                                    continue;
                                }
                                densityPoints[i + x, 16 + y, j + z] = OctNodeData.EncodeDensity(.5f);
                                typePoints[i + x, 16 + y, j + z] = (byte)index;
                            }
                        }
                    }
                    index++;
                    if (index == end) break;
                }
                if (index == end) break;
            }

        }
    
        VoxelMesh.VoxelGrid grid = new VoxelMesh.VoxelGrid(densityPoints, typePoints, Vector3Int.one * pointCount);

        return grid;
    }

    int indexFromId(int x, int y, int z, Vector3Int size) {
        return z * size.y * size.x + y * size.x + x;
    }
}

public class SphereDensitySetting {
    public Vector3 origin;
    public float radius;
    public float SphereDensity(Vector3 sample) {
        return radius - (sample - origin).magnitude;
    }
}