using System.Collections;
using System.Collections.Generic;
using ReefEditor.ContentLoading;
using ReefEditor.Octrees;
using UnityEngine;

namespace ReefEditor.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        public VoxelMesh[] meshes;
        public VoxelMesh this[Vector3Int index] {
            get {
                return meshes[GetLabel(index)];
            }
        }

        // loading fields
        public bool loadInProgress = false;
        public float loadingProgress;
        public string loadingState;

        private Vector3Int _octreeMin;
        private Vector3Int _octreeMax;
        public Vector3Int OctreeCounts {
            get {
                return _octreeMax - _octreeMin + Vector3Int.one;
            }
        }
        public Vector3Int RealSize {
            get {
                return OctreeCounts * 32;
            }
        }

        void Awake() {
            metaspace = this;
        }

        public void Create(Vector3Int octreeMin, Vector3Int octreeMax) {
            _octreeMin = octreeMin;
            _octreeMax = octreeMax;

            var regionSize = OctreeCounts;
            meshes = new VoxelMesh[regionSize.x * regionSize.y * regionSize.z];

            if (!SNContentLoader.instance.contentLoaded) {
                SNContentLoader.instance.updateMeshesOnLoad = true;
            }

            for (int y = octreeMin.y; y <= octreeMax.y; y++) {
                for (int z = octreeMin.z; z <= octreeMax.z; z++) {
                    for (int x = octreeMin.x; x <= octreeMax.x; x++) {
                        
                        Vector3Int coords = new Vector3Int(x, y, z);
                        VoxelMesh batchComponent = new GameObject().AddComponent<VoxelMesh>();
                        batchComponent.Create(transform, coords, 32);
                        meshes[GetLabel(coords)] = batchComponent;
                    }
                }   
            }

            transform.localPosition = octreeMin * -1 * 32;
        }
        public void Clear() {
            foreach (VoxelMesh mesh in meshes) {
                Destroy(mesh.gameObject);
            }
            meshes = null;
        }

        public VoxelMesh GetOctreeMesh(Vector3Int index) {
            if (OctreeExists(index)) {
                return meshes[GetLabel(index)];
            }
            return null;
        }

        public VoxelGrid GetGridForVoxel(Vector3Int voxel) => GetOctreeMesh(voxel / 32)?.grid;
        public OctNodeData GetVoxelFromOctree(Vector3Int voxel) {
            var mesh = GetOctreeMesh(voxel / 32);
            if (mesh is null) return new OctNodeData(0, 0, 0);
            return mesh.GetOctree().GetVoxel(voxel);
        }

        public void ApplyDensityAction(Brush.BrushStroke stroke) {

            List<VoxelMesh> modifiedMeshes = new List<VoxelMesh>();
            foreach(VoxelMesh mesh in meshes) {
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, mesh.MeshBounds.min, mesh.MeshBounds.max) <= stroke.brushRadius) {
                    mesh.ApplyDensityAction(stroke);
                    modifiedMeshes.Add(mesh);
                }
            }
            foreach(VoxelMesh mesh in modifiedMeshes) {
                mesh.UpdateMeshesAfterBrush(stroke);
            }
        }

        public IEnumerator<VoxelMesh> IterateThroughBatch(Vector3Int batchId) {
            for (int x = batchId.x * 5; x < (batchId.x + 1) * 5; x++) {
                for (int y = batchId.y * 5; y < (batchId.y + 1) * 5; y++) {
                    for (int z = batchId.z * 5; z < (batchId.z + 1) * 5; z++) {
                        yield return meshes[GetLabel(x, y, z)];
                    }
                }
            }
        }
        public IEnumerator<OctNode> AllOctrees() {
            for (int x = _octreeMin.x; x < _octreeMax.x; x++) {
                for (int y = _octreeMin.y; y < _octreeMax.y; y++) {
                    for (int z = _octreeMin.z; z < _octreeMax.z; z++) {
                        yield return meshes[Globals.LinearIndex(x, y, z, OctreeCounts)].GetOctree();
                    }
                }
            }
        }
        public IEnumerable<Vector3Int> BatchIndices() {
            for (int x = _octreeMin.x / 5; x < (_octreeMax.x / 5) + 1; x++) {
                for (int y = _octreeMin.y / 5; y < (_octreeMax.y / 5) + 1; y++) {
                    for (int z = _octreeMin.z / 5; z < (_octreeMax.z / 5) + 1; z++) {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public IEnumerator RegionReadCoroutine() {
            // sets + rasterizes all octrees
            loadInProgress = true;
            foreach (Vector3Int batchId in BatchIndices()) {
                loadingProgress = 0.5f;
                loadingState = $"Reading batch {batchId}";
                var nodes = BatchReadWriter.readWriter.ReadBatch(batchId);
                var meshesInBatch = IterateThroughBatch(batchId);

                while(meshesInBatch.MoveNext()) {
                    nodes.MoveNext();
                    meshesInBatch.Current.ReadRootNode(nodes.Current);
                    yield return null;
                }
            }

            yield return RegenerateAllMeshesCoroutine();
        }

        public IEnumerator RegenerateAllMeshesCoroutine() {
            
            // redistribute full grids
            loadInProgress = true;
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = 0.75f;
                loadingState = $"Joining octrees";
                mesh.UpdateFullGrid();
            }
            yield return null;

            // generate meshes
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = 0.75f;
                loadingState = $"Creating meshes";
                mesh.RegenerateMesh();
            }
            yield return null;
            
            loadInProgress = false;
        }

        public bool VoxelExists(int x, int y, int z) {
            return  x >= _octreeMin.x * 32 && x < _octreeMax.x * 32 + 32 && 
                    y >= _octreeMin.y * 32 && y < _octreeMax.y * 32 + 32 && 
                    z >= _octreeMin.z * 32 && z < _octreeMax.z * 32 + 32;
        }

        public bool OctreeExists(Vector3Int index) {
            return (index.x >= _octreeMin.x && index.x <= _octreeMax.x &&
                    index.y >= _octreeMin.y && index.y <= _octreeMax.y &&
                    index.z >= _octreeMin.z && index.z <= _octreeMax.z);
        }


        private int GetLabel(Vector3Int globalBatchIndex) {
            return GetLabel(globalBatchIndex.x, globalBatchIndex.y, globalBatchIndex.z);
        }
        private int GetLabel(int x, int y, int z) {
            int localX = x - _octreeMin.x;
            int localY = y - _octreeMin.y;
            int localZ = z - _octreeMin.z;
            return Globals.LinearIndex(localX, localY, localZ, OctreeCounts);
        }

        public byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay, int retryCount = 0) {
            // batch -> octree -> voxel
            if (retryCount == 32) return 0;

            // batch
            var voxelandPoint = transform.InverseTransformPoint(hitPoint);
            Vector3Int voxel = new Vector3Int((int)voxelandPoint.x, (int)voxelandPoint.y, (int)voxelandPoint.z);
            Vector3Int treeId = voxel / 32 + _octreeMin;
            var mesh = GetOctreeMesh(treeId);
            if (mesh == null) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            return mesh.grid.GetVoxelBlocktype(voxel);
        }
    }
}
