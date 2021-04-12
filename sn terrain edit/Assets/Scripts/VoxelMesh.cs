using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class VoxelMesh : MonoBehaviour
{
    VoxelMesh voxeland;

    static int pointsPerOctreeAxis = 32;
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

                    octreeContainers[containerI] = new PointContainer(transform, rootNodes[z, y, x], new Vector3Int(x, y, z));
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

    public void UpdateOctreeDensity() {
        for (int z = 0; z < 5; z++) {
            for (int y = 0; y < 5; y++) {
                for (int x = 0; x < 5; x++) {
                    PointContainer container = octreeContainers[Globals.LinearIndex(x, y, z, 5)];

                    //byte[] tempDensity;
                    //byte[] tempTypes;
                    //container.grid.GetFullGrids(out tempDensity, out tempTypes);
                    rootNodes[z, y, x].DeRasterizeGrid(container.grid.densityGrid, container.grid.typeGrid, 34);
                }
            }
        }
    }

    [SerializeField]
    class PointContainer {
        Vector3Int octreeIndex;

        // density data
        public Octree octree;
        public VoxelGrid grid;
        
        // other objects
        public Bounds bounds;
        GameObject meshObj;


        public PointContainer(Transform voxelandTransform, Octree _octree, Vector3Int containerIndex) {
            
            octreeIndex = containerIndex;

            Vector3 center = octreeIndex * 32 + Vector3.one * 16;
            bounds = new Bounds(center, Vector3.one * 32);
            
            octree = _octree;
            UpdateGrid();

            CreateMeshObject(voxelandTransform);
        }
        public void UpdateGrid() {

            bool doMatGallery = true;

            if (doMatGallery) {
                grid = DensityGenerator.GenerateMaterialGallery(octreeIndex, 32);
                return;
            }

            byte[] tempTypes = new byte[32 * 32 * 32];
            byte[] tempDensities = new byte[32 * 32 * 32];

            octree.Rasterize(tempDensities, tempTypes, 32);

            Vector3Int pointsPerAxis = new Vector3Int(32, 32, 32);

            grid = new VoxelGrid(Globals._1DArrayTo3D(tempDensities, Vector3Int.one * 32), 
                                 Globals._1DArrayTo3D(tempTypes, Vector3Int.one * 32),
                                 pointsPerAxis);
        } 

        void CreateMeshObject(Transform voxelandTransform) {
            meshObj = new GameObject("VoxelMesh");
            MeshFilter filter = meshObj.AddComponent<MeshFilter>();
            
            MeshRenderer renderer = meshObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Globals.GetBatchMat();
            meshObj.transform.parent = voxelandTransform.transform;
        }

        public void DensityAction_Sphere(Vector3 sphereOrigin, float sphereRadius, BrushMode mode) {

            SphereDensitySetting setting = new SphereDensitySetting() { origin = sphereOrigin, radius = sphereRadius };

            grid.ApplyDensityFunction(setting, octreeIndex * 32);
        }

        public void UpdateMesh() {

            Vector3 offset = octreeIndex * 32;

            CheckCopyOctreeSides();

            List<Mesh> containerMeshes;

            byte[] tempDensities;
            byte[] tempTypes;
            grid.GetFullGrids(out tempDensities, out tempTypes);
            
            containerMeshes = MeshBuilder.builder.GenerateMesh(tempDensities, tempTypes, grid.fullGridDim, offset);

            // update data
            if (containerMeshes.Count > 0) {
                meshObj.GetComponent<MeshFilter>().sharedMesh = containerMeshes[0];
            }

            MeshCollider coll = meshObj.GetComponent<MeshCollider>();
            if (!coll) {
                meshObj.AddComponent<MeshCollider>();
            } else {
                coll.sharedMesh = containerMeshes[0];
            }
        }

        public void CheckCopyOctreeSides() {
            VoxelMesh voxelMesh = meshObj.transform.parent.GetComponent<VoxelMesh>();
            grid.AddPaddingFull(voxelMesh, octreeIndex);
        }
    }

    public class VoxelGrid {
        public byte[,,] densityGrid;
        public byte[,,] typeGrid;
        public Vector3Int fullGridDim;

        public VoxelGrid(byte[,,] _coreDensity, byte[,,] _coreTypes, Vector3Int dimensions) {
            
            int fullSide = 32 + 2;
            densityGrid = new byte[fullSide, fullSide, fullSide];
            typeGrid = new byte[fullSide, fullSide, fullSide];;
            
            int so = 1;

            for (int z = 0; z < 32; z++) {
                for (int y = 0; y < 32; y++) {
                    for (int x = 0; x < 32; x++) {
                        densityGrid[x + so, y + so, z + so] = _coreDensity[x, y, z];
                        typeGrid[x + so, y + so, z + so] = _coreTypes[x, y, z];
                    }
                }
            }

            fullGridDim = Vector3Int.one * fullSide;
        }

        public void AddPaddingFull(VoxelMesh mesh, Vector3Int containerIndex) {

            for (int z = 0; z < 34; z++) {
                for (int y = 0; y < 34; y++) {
                    for (int x = 0; x < 34; x++) {
                        
                        Vector3Int neigIndex = containerIndex + CoreGridFromVertex(x, y, z);
                        if (GridExists(neigIndex)) {
                            VoxelGrid neigGrid = mesh.octreeContainers[Globals.LinearIndex(neigIndex.x, neigIndex.y, neigIndex.z, 5)].grid;

                            Vector3Int sample = new Vector3Int(x, y, z);
                            if (x == 0) sample.x = 32;
                            else if (x == 33) sample.x = 1;
                            
                            if (y == 0) sample.y = 32;
                            else if (y == 33) sample.y = 1;

                            if (z == 0) sample.z = 32;
                            else if (z == 33) sample.z = 1;

                            densityGrid[x, y, z] =  neigGrid.densityGrid[sample.x, sample.y, sample.z];
                            typeGrid[x, y, z] =     neigGrid.typeGrid[sample.x, sample.y, sample.z];
                        }
                    }
                }
            }
        }

        Vector3Int CoreGridFromVertex(int x, int y, int z) {
            Vector3Int offset = Vector3Int.zero;
            if (x == 0) offset.x = -1;
            else if (x == 33) offset.x = 1;

            if (y == 0) offset.y = -1;
            else if (y == 33) offset.y = 1;

            if (z == 0) offset.z = -1;
            else if (z == 33) offset.z = 1;

            return offset;
        }

        static bool GridExists(Vector3Int gridIndex) {
            return (gridIndex.x >= 0 && gridIndex.x < 5 && gridIndex.y >= 0 && gridIndex.y < 5 && gridIndex.z >= 0 && gridIndex.z < 5);
        }

        public void AddPadding(VoxelGrid fromGrid, int side) {
            
            int dir = side % 3;
            bool reverse = side / 3 == 1;
            
            int fullStart = 0, fullEnd = 34;
            int coreEnd = 32, coreStart = 2;

            if (dir == 0) {
                int myX = (reverse ? fullStart : fullEnd);
                int fromX = (reverse ? coreEnd : coreStart);

                for (int y = 2; y < 34; ++y) {
                    for (int z = 2; z < 34; ++z) {
                        densityGrid[myX, y, z] = fromGrid.densityGrid[fromX, y, z];
                        typeGrid[myX, y, z] = fromGrid.typeGrid[fromX, y, z];

                        densityGrid[myX + 1, y, z] = fromGrid.densityGrid[fromX + 1, y, z];
                        typeGrid[myX + 1, y, z] = fromGrid.typeGrid[fromX + 1, y, z];
                    }
                }
            }
            else if (dir == 1) {
                int myY = (reverse ? fullStart : fullEnd);
                int fromY = (reverse ? coreEnd : coreStart);

                for (int x = 2; x < 34; ++x) {
                    for (int z = 2; z < 34; ++z) {
                        densityGrid[x, myY, z] = fromGrid.densityGrid[x, fromY, z];
                        typeGrid[x, myY, z] = fromGrid.typeGrid[x, fromY, z];
                        
                        densityGrid[x, myY + 1, z] = fromGrid.densityGrid[x, fromY + 1, z];
                        typeGrid[x, myY + 1, z] = fromGrid.typeGrid[x, fromY + 1, z];
                    }
                }
            }
            else if (dir == 2) {
                int myZ = (reverse ? fullStart : fullEnd);
                int fromZ = (reverse ? coreEnd : coreStart);

                for (int x = 2; x < 34; ++x) {
                    for (int y = 2; y < 34; ++y) {
                        densityGrid[x, y, myZ] = fromGrid.densityGrid[x, y, fromZ];
                        typeGrid[x, y, myZ] = fromGrid.typeGrid[x, y, fromZ];
                        
                        densityGrid[x, y, myZ + 1] = fromGrid.densityGrid[x, y, fromZ + 1];
                        typeGrid[x, y, myZ + 1] = fromGrid.typeGrid[x, y, fromZ + 1];
                    }
                }
            }
        }

        public void GetFullGrids(out byte[] fullDensityGrid, out byte[] fullTypeGrid) {
            fullDensityGrid =   Globals._3DArrayTo1D(densityGrid);
            fullTypeGrid =      Globals._3DArrayTo1D(typeGrid);
        }

        public void ApplyDensityFunction(SphereDensitySetting setting, Vector3 gridOrigin) {
            for (int z = 1; z < 33; z++) {
                for (int y = 1; y < 33; y++) {
                    for (int x = 1; x < 33; x++) {
                        byte oldDist = densityGrid[x, y, z], oldType = typeGrid[x, y, z];

                        float functionDensity = setting.SphereDensity(new Vector3(x, y, z) + gridOrigin);
                        float clampedFunctionDensity = Mathf.Clamp(functionDensity, -1, 1);
                        byte encodedFunctionDensity = OctNodeData.EncodeDensity(clampedFunctionDensity);

                        byte compareDist = oldDist;
                        if (oldDist == 0 && oldType != 0) compareDist = byte.MaxValue;

                        if (encodedFunctionDensity > compareDist) {
                            // change value
                            bool nodeIsFar = functionDensity != clampedFunctionDensity;
                            if (nodeIsFar) encodedFunctionDensity = 0;

                            byte newType = clampedFunctionDensity > 0 ? Brush.selectedType : (byte)0;

                            densityGrid[x, y, z] = encodedFunctionDensity;
                            typeGrid[x, y, z] = newType;
                        }
                    }
                }
            }
        }
    }
}