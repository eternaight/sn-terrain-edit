using UnityEngine;
using ReefEditor.Octrees;
using ReefEditor.ContentLoading;

namespace ReefEditor.VoxelTech {
    public class VoxelMesh : MonoBehaviour {

        private OctNode rootNode;
        public Vector3Int index;
        public VoxelGrid grid;
        public Bounds MeshBounds { get; set; }

        private const int GRID_RESOLUTION = 32;
        private const int MAX_OCTREE_HEIGHT = 4;

        public void Create(Transform parent, Vector3Int globalOctreeIndex, int realSize) {

            index = globalOctreeIndex;
            grid = new VoxelGrid(index, GRID_RESOLUTION);

            SetupGameObject(parent, realSize);
        }
        private void SetupGameObject(Transform parent, float realSize) {
            gameObject.name = $"octree-{index.x}-{index.y}-{index.z}";
            transform.localPosition = (Vector3)index * realSize;
            gameObject.layer = 0;
            transform.SetParent(parent);

            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            MeshBounds = new Bounds(Vector3.one * realSize / 2f, Vector3.one * realSize);
        }

        public void RegenerateMesh() { 
            if (grid == null) return;

            var mesh = grid.GenerateMesh(out int[] blocktypes);

            // update data
            if (mesh.triangles.Length > 0) {
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                gameObject.AddComponent<MeshCollider>();

                Material[] materials = new Material[blocktypes.Length];
                for (int b = 0; b < blocktypes.Length; b++) {
                    materials[b] = SNContentLoader.GetMaterialForType(blocktypes[b]);
                }
                gameObject.GetComponent<MeshRenderer>().materials = materials;
            } else {
                //Debug.Log($"Received empty mesh for object {gameObject.name}");
            }
        }

        public void UpdateFullGrid() {
            grid.UpdateFullGrid();
        }
        public void ApplyDensityAction(Brush.BrushStroke stroke) {
        }
        public void UpdateMeshesAfterBrush(Brush.BrushStroke stroke) {
        }

        public void UpdateOctreeDensity() {
            rootNode.DeRasterizeGrid(grid, 0, MAX_OCTREE_HEIGHT);
        }

        public void ReadRootNode(OctNode root) {
            rootNode = root;
        }
        public OctNode GetOctree() => rootNode;
    }
}