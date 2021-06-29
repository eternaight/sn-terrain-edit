using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class VoxelMesh : MonoBehaviour
{
    public static int LEVEL_OF_DETAIL { get; private set; } //0-5 -> 32-1 side
    const int OCTREE_SIDE = 32;
    public const int CONTAINERS_PER_SIDE = 5;

    public static int RESOLUTION {
        get {
            return (int)Mathf.Pow(2, 5 - LEVEL_OF_DETAIL);
        }
    }

    [SerializeField] DensityGenerator density;
    [SerializeField] MeshBuilder mesh;

    Octree[,,] rootNodes;
    PointContainer[] octreeContainers;

    public void Init(Octree[,,] _rootNodes, int _lod) {

        int _totalContainers = CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE;
        LEVEL_OF_DETAIL = Mathf.Clamp(_lod, 0, 5);

        rootNodes = _rootNodes;
        bool _firstInit = octreeContainers == null;
        if (_firstInit) {
            octreeContainers = new PointContainer[_totalContainers];
        }

        for (int z = 0; z < CONTAINERS_PER_SIDE; z++) {
            for (int y = 0; y < CONTAINERS_PER_SIDE; y++) {
                for (int x = 0; x < CONTAINERS_PER_SIDE; x++) {
                    
                    int containerI = x + y * CONTAINERS_PER_SIDE + z * CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE;

                    if (_firstInit) {
                        octreeContainers[containerI] = new PointContainer(transform, new Vector3Int(x, y, z));
                    }
                    octreeContainers[containerI].SetOctree(rootNodes[z, y, x]);
                }
            }
        }

        RefreshContainerMeshes();
        if (!SNContentLoader.instance.contentLoaded) {
            SNContentLoader.instance.OnContentLoaded += RefreshContainerMeshes;
        }
    }

    void RefreshContainerMeshes() {
        for (int i = 0; i < octreeContainers.Length; i++) {
            octreeContainers[i].UpdateMesh();
        }
    }

    public void DensityAction_Sphere(Vector3 _sphereOrigin, float _sphereRadius, BrushMode _mode) {

        Vector3Int hitChunk = new Vector3Int((int)_sphereOrigin.x / RESOLUTION, (int)_sphereOrigin.y / RESOLUTION, (int)_sphereOrigin.z / RESOLUTION);

        for (int k = 0; k < CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE; k++) {
            Bounds bounds = octreeContainers[k].bounds;
            if (OctreeRaycasting.DistanceToBox(_sphereOrigin, bounds.min, bounds.max) <= _sphereRadius) {
                octreeContainers[k].DensityAction_Sphere(_sphereOrigin, _sphereRadius, _mode);
                octreeContainers[k].UpdateMesh();
            }
        }
    }

    public void UpdateOctreeDensity() {
        for (int z = 0; z < 5; z++) {
            for (int y = 0; y < 5; y++) {
                for (int x = 0; x < 5; x++) {
                    PointContainer _container = octreeContainers[Globals.LinearIndex(x, y, z, CONTAINERS_PER_SIDE)];
                    rootNodes[z, y, x].DeRasterizeGrid(_container.grid.densityGrid, _container.grid.typeGrid, RESOLUTION + 2, 5 - LEVEL_OF_DETAIL);
                }
            }
        }
    }

    public byte SampleBlocktype(Vector3 _point, Ray _cameraRay, int _retryCount = 0) {
        
        if (_retryCount == 4) return 0;

        int x = (int)_point.x / RESOLUTION;
        int y = (int)_point.y / RESOLUTION;
        int z = (int)_point.z / RESOLUTION;

        byte type = octreeContainers[Globals.LinearIndex(x, y, z, 5)].SampleBlocktype(_point);

        if (type == 0) {
            float newDistance = Vector3.Distance(_point, _cameraRay.origin) + .5f;
            Vector3 newPoint = _cameraRay.GetPoint(newDistance);

            return SampleBlocktype(newPoint, _cameraRay, _retryCount + 1);
        }

        return type;
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

        public PointContainer(Transform _voxelandTf, Vector3Int _index) {
            octreeIndex = _index;
            bounds = new Bounds(octreeIndex * RESOLUTION + Vector3.one * RESOLUTION / 2, Vector3.one * RESOLUTION);

            CreateMeshObject(_voxelandTf);
        }
        public void SetOctree(Octree _octree) {
            octree = _octree;
            UpdateGrid();
        }
        public void UpdateGrid() {

            int _res = RESOLUTION;
            byte[] tempTypes = new byte[_res * _res * _res];
            byte[] tempDensities = new byte[_res * _res * _res];

            octree.Rasterize(tempDensities, tempTypes, RESOLUTION, 5 - LEVEL_OF_DETAIL);

            grid = new VoxelGrid(Globals.LineToCubeArray(tempDensities, Vector3Int.one * RESOLUTION), 
                                 Globals.LineToCubeArray(tempTypes, Vector3Int.one * RESOLUTION));
        } 

        void CreateMeshObject(Transform _voxelandTf) {
            meshObj = new GameObject("VoxelMesh");
            meshObj.AddComponent<MeshFilter>();
            meshObj.AddComponent<MeshRenderer>();
            meshObj.transform.SetParent(_voxelandTf);
            meshObj.transform.localPosition = Vector3.zero;
        }

        public void DensityAction_Sphere(Vector3 _sphereOrigin, float _sphereRadius, BrushMode _mode) {
            SphereDensitySetting setting = new SphereDensitySetting() { origin = _sphereOrigin, radius = _sphereRadius };
            grid.ApplyDensityFunction(setting, octreeIndex * RESOLUTION, _mode);
        }

        public void UpdateMesh() {
            CheckCopyOctreeSides();

            byte[] _tempDensities;
            byte[] _tempTypes;
            grid.GetFullGrids(out _tempDensities, out _tempTypes);
            
            int[] blocktypes;
            Vector3 offset = octreeIndex * RESOLUTION;
            List<Mesh> _containerMeshes = MeshBuilder.builder.GenerateMesh(_tempDensities, _tempTypes, grid.fullGridDim, offset, out blocktypes);

            // update data
            if (_containerMeshes.Count > 0) {
                meshObj.GetComponent<MeshFilter>().sharedMesh = _containerMeshes[0];

                MeshCollider coll = meshObj.GetComponent<MeshCollider>();
                if (!coll) {
                    meshObj.AddComponent<MeshCollider>();
                } else {
                    coll.sharedMesh = _containerMeshes[0];
                }

                MeshRenderer renderer = meshObj.GetComponent<MeshRenderer>();
                Material[] materials = new Material[blocktypes.Length];
                for (int b = 0; b < blocktypes.Length; b++) {
                    materials[b] = SNContentLoader.GetMaterialForType(blocktypes[b]);
                }
                renderer.materials = materials;
            }
        }

        public void CheckCopyOctreeSides() {
            VoxelMesh voxelMesh = meshObj.transform.parent.GetComponent<VoxelMesh>();
            grid.AddPaddingFull(voxelMesh, octreeIndex);
        }

        public byte SampleBlocktype(Vector3 worldPoint) {
            Vector3 localPoint = worldPoint - octreeIndex * RESOLUTION;
            int x = (int)localPoint.x;
            int y = (int)localPoint.y;
            int z = (int)localPoint.z;

            return grid.typeGrid[x + 1, y + 1, z + 1];
        }
    }

    public class VoxelGrid {
        public byte[,,] densityGrid;
        public byte[,,] typeGrid;
        public Vector3Int fullGridDim;

        public VoxelGrid(byte[,,] _coreDensity, byte[,,] _coreTypes) {
            
            int _fullSide = RESOLUTION + 2;
            densityGrid = new byte[_fullSide, _fullSide, _fullSide];
            typeGrid = new byte[_fullSide, _fullSide, _fullSide];;
            
            int so = 1;

            for (int z = 0; z < RESOLUTION; z++) {
                for (int y = 0; y < RESOLUTION; y++) {
                    for (int x = 0; x < RESOLUTION; x++) {
                        densityGrid[x + so, y + so, z + so] = _coreDensity[x, y, z];
                        typeGrid[x + so, y + so, z + so] = _coreTypes[x, y, z];
                    }
                }
            }

            fullGridDim = Vector3Int.one * _fullSide;
        }

        public void AddPaddingFull(VoxelMesh mesh, Vector3Int containerIndex) {

            int _fullSide = RESOLUTION + 2;
            for (int z = 0; z < _fullSide; z++) {
                for (int y = 0; y < _fullSide; y++) {
                    for (int x = 0; x < _fullSide; x++) {
                        
                        Vector3Int neigIndex = containerIndex + CoreGridFromVertex(x, y, z);
                        if (GridExists(neigIndex)) {
                            VoxelGrid neigGrid = mesh.octreeContainers[Globals.LinearIndex(neigIndex.x, neigIndex.y, neigIndex.z, CONTAINERS_PER_SIDE)].grid;

                            Vector3Int sample = new Vector3Int(x, y, z);
                            if (x == 0) sample.x = RESOLUTION;
                            else if (x == RESOLUTION + 1) sample.x = 1;
                            
                            if (y == 0) sample.y = RESOLUTION;
                            else if (y == RESOLUTION + 1) sample.y = 1;

                            if (z == 0) sample.z = RESOLUTION;
                            else if (z == RESOLUTION + 1) sample.z = 1;

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
            else if (x == RESOLUTION + 1) offset.x = 1;

            if (y == 0) offset.y = -1;
            else if (y == RESOLUTION + 1) offset.y = 1;

            if (z == 0) offset.z = -1;
            else if (z == RESOLUTION + 1) offset.z = 1;

            return offset;
        }

        static bool GridExists(Vector3Int gridIndex) {
            return (gridIndex.x >= 0 && gridIndex.x < CONTAINERS_PER_SIDE && gridIndex.y >= 0 && gridIndex.y < CONTAINERS_PER_SIDE && gridIndex.z >= 0 && gridIndex.z < CONTAINERS_PER_SIDE);
        }

        public void GetFullGrids(out byte[] _fullDensityGrid, out byte[] _fullTypeGrid) {
            _fullDensityGrid =   Globals.CubeToLineArray(densityGrid);
            _fullTypeGrid =      Globals.CubeToLineArray(typeGrid);
        }

        public void ApplyDensityFunction(SphereDensitySetting _setting, Vector3 _gridOrigin, BrushMode _mode) {
            for (int z = 1; z < RESOLUTION + 1; z++) {
                for (int y = 1; y < RESOLUTION + 1; y++) {
                    for (int x = 1; x < RESOLUTION + 1; x++) {
                        byte[] new_values = _setting.SphereDensityAction(_mode, densityGrid[x, y, z], typeGrid[x, y, z], new Vector3(x, y, z) + _gridOrigin);
                        densityGrid[x, y, z] = new_values[0];
                        typeGrid[x, y, z] = new_values[1];
                    }
                }
            }
        }
    }
}