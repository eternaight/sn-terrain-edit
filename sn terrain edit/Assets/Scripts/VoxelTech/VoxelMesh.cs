using UnityEngine;
using ReefEditor.Octrees;
using ReefEditor.ContentLoading;

namespace ReefEditor.VoxelTech {
    public class VoxelMesh : MonoBehaviour {

        internal PointContainer[] octreeContainers;
        public Octree[,,] nodes;
        public Vector3Int batchIndex;
        public Vector3Int octreeCounts;

        public void Create(Vector3Int _batchIndex) {

            batchIndex = _batchIndex;
            SetupGameObject();

            octreeCounts = Vector3Int.one * VoxelWorld.CONTAINERS_PER_SIDE;
            if (_batchIndex.x == 25) octreeCounts.x = 3;
            if (_batchIndex.z == 25) octreeCounts.z = 3;

            octreeContainers = new PointContainer[octreeCounts.x * octreeCounts.y * octreeCounts.z];

            for (int z = 0; z < octreeCounts.z; z++) {
                for (int y = 0; y < octreeCounts.y; y++) {
                    for (int x = 0; x < octreeCounts.x; x++) {
                        octreeContainers[Globals.LinearIndex(x, y, z, octreeCounts)] = new PointContainer(transform, new Vector3Int(x, y, z), batchIndex);
                    }
                }
            }
        }
        private void SetupGameObject() {
            const int octreeSide = VoxelWorld.OCTREE_SIDE;
            transform.position = (batchIndex - VoxelWorld.start) * octreeSide * 5;

            BoxCollider coll = gameObject.AddComponent<BoxCollider>();
            gameObject.layer = 1;

            coll.center = (Vector3)octreeCounts * octreeSide / 2f;
            coll.size = octreeCounts * octreeSide;
            coll.isTrigger = true;
        }

        public bool OctreesReadCallback(Octree[,,] _nodes) {
            nodes = _nodes;
            for (int z = 0; z < octreeCounts.z; z++) {
                for (int y = 0; y < octreeCounts.y; y++) {
                    for (int x = 0; x < octreeCounts.x; x++) {
                        octreeContainers[Globals.LinearIndex(x, y, z, octreeCounts)].SetOctree(nodes[z, y, x]);
                    }
                }
            }

            return true;
        }
        public void Regenerate() {
            for (int i = 0; i < octreeContainers.Length; i++) {
                octreeContainers[i].UpdateMesh();
            }
        }

        public VoxelGrid GetVoxelGrid(Vector3Int containerIndex) {
            return octreeContainers[Globals.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, octreeCounts)].grid;
        }
        public GameObject GetContainerObject(Vector3Int containerIndex) {
            return octreeContainers[Globals.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, octreeCounts)].meshObj;
        }

        public void UpdateFullGrids() {
            foreach (PointContainer container in octreeContainers) {
                container.UpdateFullGrid();
            }
        }

        public void Write() => BatchReadWriter.readWriter.WriteOptoctrees(batchIndex, nodes);

        public void CacheGridsInsideBrush(Brush.BrushStroke stroke) {
            foreach (PointContainer container in octreeContainers) {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, bounds.min, bounds.max) <= stroke.brushRadius) {
                    container.CacheGrid();
                }
            }
        }
        public void ApplyDensityAction(Brush.BrushStroke stroke) {
            Vector3 brushPosition = stroke.brushLocation - transform.position;

            foreach (PointContainer container in octreeContainers) {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.DistanceToBox(brushPosition, bounds.min, bounds.max) <= stroke.brushRadius) {
                    container.ApplyDensityAction(stroke);
                }
            }
        }
        public void UpdateMeshesAfterBrush(Brush.BrushStroke stroke) {
            Vector3 brushPosition = stroke.brushLocation - transform.position;
            foreach (PointContainer container in octreeContainers) {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.DistanceToBox(brushPosition, bounds.min, bounds.max) <= stroke.brushRadius) {
                    container.UpdateFullGrid();
                    container.UpdateMesh();
                }
            }
        }

        public void UpdateOctreeDensity() {
            for (int z = 0; z < 5; z++) {
                for (int y = 0; y < 5; y++) {
                    for (int x = 0; x < 5; x++) {
                        PointContainer _container = octreeContainers[Globals.LinearIndex(x, y, z, octreeCounts)];
                        nodes[z, y, x].DeRasterizeGrid(_container.grid.densityGrid, _container.grid.typeGrid, VoxelWorld.RESOLUTION + 2, 5 - VoxelWorld.LEVEL_OF_DETAIL);
                    }
                }
            }
        }

        internal class PointContainer {
            Vector3Int batchIndex;
            Vector3Int octreeIndex;

            // density data
            public Octree octree;
            public VoxelGrid grid;
            
            // other objects
            public Bounds bounds;
            public GameObject meshObj;

            public Mesh mesh => meshObj.GetComponent<MeshFilter>().mesh;

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

                grid = new VoxelGrid(tempDensities, tempTypes, octreeIndex, batchIndex);
            } 
            
            public void CacheGrid() => grid.Cache();

            public void ApplyDensityAction(Brush.BrushStroke stroke) {
                if (grid != null)
                    grid.ApplyDensityFunction(stroke, octreeIndex * VoxelWorld.OCTREE_SIDE + meshObj.transform.position);
            }

            public void UpdateMesh() {
                byte[] _tempDensities;
                byte[] _tempTypes;
                if (grid == null) return;
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

            public void UpdateFullGrid() {
                if (grid != null)
                    grid.UpdateFullGrid();
            }

            public byte SampleBlocktype(Vector3 worldPoint) {
                Vector3 localPoint = worldPoint - octreeIndex * VoxelWorld.RESOLUTION - meshObj.transform.parent.position;
                int x = (int)localPoint.x;
                int y = (int)localPoint.y;
                int z = (int)localPoint.z;

                return VoxelGrid.GetVoxel(grid.typeGrid, x + 1, y + 1, z + 1);
            }
        }
    }
}