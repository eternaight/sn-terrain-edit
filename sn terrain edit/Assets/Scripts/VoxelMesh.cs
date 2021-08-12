using System.Collections.Generic;
using UnityEngine;
using ReefEditor.Octrees;
using ReefEditor.ContentLoading;

namespace ReefEditor.VoxelTech {
    public class VoxelMesh : MonoBehaviour {

        internal PointContainer[] octreeContainers;
        public Octree[,,] nodes;
        public Vector3Int batchIndex;

        public void Create(Vector3Int _batchIndex) {

            batchIndex = _batchIndex;
            SetupGameObject();

            const int octreeSide = VoxelWorld.CONTAINERS_PER_SIDE;
            const int octreesTotal = octreeSide * octreeSide * octreeSide;

            octreeContainers = new PointContainer[octreesTotal];

            for (int z = 0; z < octreeSide; z++) {
                for (int y = 0; y < octreeSide; y++) {
                    for (int x = 0; x < octreeSide; x++) {
                        int containerI = x + y * octreeSide + z * octreeSide * octreeSide;
                        octreeContainers[containerI] = new PointContainer(transform, new Vector3Int(x, y, z), batchIndex);
                    }
                }
            }
        }
        private void SetupGameObject() {
            const int octreeSide = VoxelWorld.OCTREE_SIDE;
            transform.position = (batchIndex - VoxelWorld.start) * octreeSide * 5;

            BoxCollider coll = gameObject.AddComponent<BoxCollider>();
            gameObject.layer = 1;

            coll.center = new Vector3(octreeSide * 2.5f, octreeSide * 2.5f, octreeSide * 2.5f);
            coll.size = new Vector3(octreeSide * 5, octreeSide * 5, octreeSide * 5);
            coll.isTrigger = true;
        }

        public void OctreesReadCallback(Octree[,,] _nodes) {

            if (_nodes == null) return;

            const int octreeSide = VoxelWorld.CONTAINERS_PER_SIDE;

            for (int z = 0; z < octreeSide; z++) {
                for (int y = 0; y < octreeSide; y++) {
                    for (int x = 0; x < octreeSide; x++) {
                        int containerI = x + y * octreeSide + z * octreeSide * octreeSide;
                        octreeContainers[containerI].SetOctree(_nodes[z, y, x]);
                    }
                }
            }
        }
        public void Regenerate() {
            for (int i = 0; i < octreeContainers.Length; i++) {
                octreeContainers[i].UpdateMesh();
            }
        }

        public VoxelGrid GetVoxelGrid(Vector3Int containerIndex) {
            return octreeContainers[Globals.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, VoxelWorld.CONTAINERS_PER_SIDE)].grid;
        }

        public void UpdateFullGrids() {
            foreach (PointContainer container in octreeContainers) {
                container.UpdateFullGrid();
            }
        }

        public void Write() {
            throw new System.NotImplementedException();
        }

        public void ApplyDensityAction(Brush.BrushStroke stroke) {
            
            const int CONTAINERS_PER_SIDE = VoxelWorld.CONTAINERS_PER_SIDE;
            int RESOLUTION = VoxelWorld.RESOLUTION;

            Vector3 brushPosition = stroke.brushLocation - transform.position;
            Vector3Int hitChunk = new Vector3Int((int)brushPosition.x / RESOLUTION, (int)brushPosition.y / RESOLUTION, (int)brushPosition.z / RESOLUTION);

            for (int k = 0; k < CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE * CONTAINERS_PER_SIDE; k++) {
                Bounds bounds = octreeContainers[k].bounds;
                if (OctreeRaycasting.DistanceToBox(brushPosition, bounds.min, bounds.max) <= stroke.brushRadius) {
                    octreeContainers[k].ApplyDensityAction(stroke);
                    octreeContainers[k].UpdateMesh();
                }
            }
        }

        public void UpdateOctreeDensity() {
            for (int z = 0; z < 5; z++) {
                for (int y = 0; y < 5; y++) {
                    for (int x = 0; x < 5; x++) {
                        PointContainer _container = octreeContainers[Globals.LinearIndex(x, y, z, VoxelWorld.CONTAINERS_PER_SIDE)];
                        nodes[z, y, x].DeRasterizeGrid(_container.grid.densityGrid, _container.grid.typeGrid, VoxelWorld.RESOLUTION + 2, 5 - VoxelWorld.LEVEL_OF_DETAIL);
                    }
                }
            }
        }

        public byte SampleBlocktype(Vector3 _point, Ray _cameraRay, int _retryCount = 0) {
            
            if (_retryCount == 4) return 0;

            Vector3 _local = _point - transform.position; 
            int x = (int)_local.x / VoxelWorld.RESOLUTION;
            int y = (int)_local.y / VoxelWorld.RESOLUTION;
            int z = (int)_local.z / VoxelWorld.RESOLUTION;

            byte type = octreeContainers[Globals.LinearIndex(x, y, z, 5)].SampleBlocktype(_point);

            if (type == 0) {
                float newDistance = Vector3.Distance(_point, _cameraRay.origin) + .5f;
                Vector3 newPoint = _cameraRay.GetPoint(newDistance);

                return SampleBlocktype(newPoint, _cameraRay, _retryCount + 1);
            }

            return type;
        }

        internal class PointContainer {
            Vector3Int batchIndex;
            Vector3Int octreeIndex;

            // density data
            public Octree octree;
            public VoxelGrid grid;
            
            // other objects
            public Bounds bounds;
            GameObject meshObj;

            public PointContainer(Transform _voxelandTf, Vector3Int _octreeIndex, Vector3Int _batchIndex) {
                octreeIndex = _octreeIndex;
                batchIndex = _batchIndex;
                int fullGridSide = VoxelWorld.RESOLUTION + 2;
                // assume bounds has a center relative to game object origin
                bounds = new Bounds(octreeIndex * VoxelWorld.RESOLUTION + Vector3.one * fullGridSide / 2, Vector3.one * fullGridSide);

                CreateMeshObject(_voxelandTf);
            }
            void CreateMeshObject(Transform _voxelandTf) {
                meshObj = new GameObject("OctreeMesh");
                meshObj.AddComponent<MeshFilter>();
                meshObj.AddComponent<MeshRenderer>();
                meshObj.transform.SetParent(_voxelandTf);
                meshObj.transform.localPosition = Vector3.zero;
            }
            public void SetOctree(Octree _octree) {
                octree = _octree;
                RasterizeOctree();
            }
            public void RasterizeOctree() {

                int _res = VoxelWorld.RESOLUTION;
                byte[] tempTypes = new byte[_res * _res * _res];
                byte[] tempDensities = new byte[_res * _res * _res];

                octree.Rasterize(tempDensities, tempTypes, _res, 5 - VoxelWorld.LEVEL_OF_DETAIL);

                grid = new VoxelGrid(Globals.LineToCubeArray(tempDensities, Vector3Int.one * _res), 
                                    Globals.LineToCubeArray(tempTypes, Vector3Int.one * _res), octreeIndex, batchIndex);
            } 


            public void ApplyDensityAction(Brush.BrushStroke stroke) {
                grid.ApplyDensityFunction(stroke, octreeIndex * VoxelWorld.RESOLUTION + meshObj.transform.position);
            }

            public void UpdateMesh() {
                byte[] _tempDensities;
                byte[] _tempTypes;
                grid.GetFullGrids(out _tempDensities, out _tempTypes);
                
                int[] blocktypes;
                Vector3 offset = octreeIndex * VoxelWorld.RESOLUTION;
                Mesh containerMesh = MeshBuilder.builder.GenerateMesh(_tempDensities, _tempTypes, grid.fullGridDim, offset, out blocktypes);

                // update data
                if (containerMesh.triangles.Length > 0) {
                    meshObj.GetComponent<MeshFilter>().sharedMesh = containerMesh;

                    MeshCollider coll = meshObj.GetComponent<MeshCollider>();
                    if (!coll) {
                        meshObj.AddComponent<MeshCollider>();
                    } else {
                        coll.sharedMesh = containerMesh;
                    }

                    MeshRenderer renderer = meshObj.GetComponent<MeshRenderer>();
                    Material[] materials = new Material[blocktypes.Length];
                    for (int b = 0; b < blocktypes.Length; b++) {
                        materials[b] = SNContentLoader.GetMaterialForType(blocktypes[b]);
                    }
                    renderer.materials = materials;
                }
            }

            public void UpdateFullGrid() => grid.UpdateFullGrid();

            public byte SampleBlocktype(Vector3 worldPoint) {
                Vector3 localPoint = worldPoint - octreeIndex * VoxelWorld.RESOLUTION - meshObj.transform.parent.position;
                int x = (int)localPoint.x;
                int y = (int)localPoint.y;
                int z = (int)localPoint.z;

                return grid.typeGrid[x + 1, y + 1, z + 1];
            }
        }
    }
}