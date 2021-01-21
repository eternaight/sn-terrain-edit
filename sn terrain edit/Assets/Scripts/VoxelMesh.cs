    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class VoxelMesh : MonoBehaviour
{
    VoxelMesh voxeland;

    static int pointsPerOctreeAxis = 33;
    public int containersPerSide = 5;

    [SerializeField] DensityGenerator density;
    [SerializeField] MeshBuilder mesh;

    Octree[,,] rootNodes;
    PointContainer[] octreeContainers;

    public void Init(Octree[,,] _rootNodes) {

        voxeland = this;
        int totalContainers = containersPerSide * containersPerSide * containersPerSide;

        rootNodes = _rootNodes;
        octreeContainers = new PointContainer[totalContainers];

        for (int z = 0; z < containersPerSide; z++) {
            for (int y = 0; y < containersPerSide; y++) {
                for (int x = 0; x < containersPerSide; x++) {
                    
                    int containerI = x + y * containersPerSide + z * containersPerSide * containersPerSide;

                    int densitySide = pointsPerOctreeAxis;
                    float[] containerDensity = new float[densitySide * densitySide * densitySide];
                    Queue<DensityCube> densityCubes = rootNodes[z, y, x].FillDensityArray(densitySide);

                    foreach(DensityCube dube in densityCubes) {
                        for (int i = dube.start.x; i < dube.start.x + dube.size; i++) {
                            for (int j = dube.start.y; j < dube.start.y + dube.size; j++) {
                                for (int k = dube.start.z; k < dube.start.z + dube.size; k++) {
                                    containerDensity[k * densitySide * densitySide + j * densitySide + i] = dube.densityValue;
                                }
                            }
                        }
                    }

                    octreeContainers[containerI] = new PointContainer(transform, containerDensity, new Vector3Int(x, y, z));
                }
            }
        }

        for (int i = 0; i < totalContainers; i++) {
            octreeContainers[i].UpdateMesh();
        }
    }

    public void DensityAction_Sphere(Vector3 sphereOrigin, float sphereRadius, BrushMode mode) {

        Vector3Int hitChunk = new Vector3Int((int)sphereOrigin.x / 32, (int)sphereOrigin.y / 32, (int)sphereOrigin.z / 32);

        for (int k = 0; k < 125; k++) {
            
            Bounds bounds = octreeContainers[k].bounds;
            if (OctreeRaycasting.DistanceToBox(sphereOrigin, bounds.min, bounds.max) <= sphereRadius) {
                octreeContainers[k].DensityAction_Sphere(sphereOrigin, sphereRadius, mode);
                octreeContainers[k].UpdateMesh();
            }

        }
    }

    [SerializeField]
    class PointContainer {
        public Vector3Int pointsPerAxis;
        Vector3Int octreeIndex;

        // density data
        public float[] density;
        
        // other objects
        public Bounds bounds;
        GameObject meshObj;


        public PointContainer(Transform voxelandTransform, float[] density, Vector3Int containerIndex) {
            
            int side = pointsPerOctreeAxis;
            octreeIndex = containerIndex;

            pointsPerAxis = new Vector3Int(side, side, side);
            
            // This adds border points to have a closed mesh - may cause problems if there's more than 1 batch
            // TODO: add support for 2+ batches (add border points only on border batches?)
            // if (octreeIndex.x == 4) pointsPerAxis.x += 1;
            // if (octreeIndex.y == 4) pointsPerAxis.y += 1;
            // if (octreeIndex.z == 4) pointsPerAxis.z += 1;

            Vector3 center = octreeIndex * 32 + Vector3.one * 16;
            bounds = new Bounds(center, Vector3.one * 32);

            this.density = density;

            CreateMeshObject(voxelandTransform);
        }

        void CreateMeshObject(Transform voxelandTransform) {
            meshObj = new GameObject("VoxelMesh");
            MeshFilter filter = meshObj.AddComponent<MeshFilter>();
            
            MeshRenderer renderer = meshObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Globals.GetBatchMat();
            meshObj.transform.parent = voxelandTransform.transform;
        }

        public void DensityAction_Sphere(Vector3 sphereOrigin, float sphereRadius, BrushMode mode) {

            throw new System.NotImplementedException();

            //SphereDensitySetting setting = new SphereDensitySetting() { origin = sphereOrigin, radius = sphereRadius };

            //octree.ApplyDensityFunction(setting);
            //UpdateMesh();
        }

        public void UpdateMesh() {
            
            int pointsSide = VoxelMesh.pointsPerOctreeAxis;

            int[] pointTypes = new int[density.Length];

            Vector3Int pointCloudSize = Vector3Int.one * pointsSide;
            Vector3 offset = octreeIndex * 32;

            DensityGenerator.density.ApplyEdgeDensity(density, pointTypes, pointCloudSize, octreeIndex);
            List<Mesh> containerMeshes = MeshBuilder.builder.MeshFromPoints(density, pointCloudSize, offset);

            // update data
            if (containerMeshes.Count > 0) {
                meshObj.GetComponent<MeshFilter>().sharedMesh = containerMeshes[0];
            }

            meshObj.AddComponent<MeshCollider>();
        }
    }
}

public class DensityContainer {
    public float[] density;
    public int side;
    public DensityContainer(int side) {
        density = new float[side * side * side];
    }
}
