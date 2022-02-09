using UnityEngine;
using ReefEditor.VoxelEditing;
using ReefEditor.ContentLoading;

namespace ReefEditor.Streaming {
    public class OctreeMesh : MonoBehaviour {

        public Vector3Int index;
        public int currentHeight;
        public Bounds MeshBounds { get; set; }

        public void Setup(Transform parent, Vector3Int globalOctreeIndex, int realSize) {
            index = globalOctreeIndex;

            gameObject.name = $"octree-{index.x}-{index.y}-{index.z}";
            gameObject.layer = 0;
            transform.SetParent(parent);
            transform.localPosition = (Vector3)index * realSize;

            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshCollider>();
            
            MeshBounds = new Bounds(Vector3.one * realSize / 2f, Vector3.one * realSize);
        }

        public void ReloadMesh(int maxOctreeHeight, int biggestNode) {
            currentHeight = maxOctreeHeight;

            maxOctreeHeight = Mathf.Clamp(maxOctreeHeight, 0, 5);
            int resolution = biggestNode >> (5 - maxOctreeHeight);

            var arraySize = Vector3Int.one * (resolution + 2);
            var worldOriginVoxel = index * resolution - Vector3Int.one;
            int leng = arraySize.x * arraySize.y * arraySize.z;
            var densityGrid = new byte[leng];
            var typeGrid = new byte[leng];

            for (int z = 0; z < arraySize.z; z++) {
                for (int y = 0; y < arraySize.y; y++) {
                    for (int x = 0; x < arraySize.x; x++) {
                        // 5-1, 4-2, 3-4, ... 0-32 
                        var localVoxel = new Vector3Int(x, y, z) * (1 << (5 - maxOctreeHeight));
                        var voxel = VoxelMetaspace.instance.GetOctnodeVoxel(worldOriginVoxel + localVoxel, maxOctreeHeight);
                        var id = Globals.LinearIndex(x, y, z, arraySize);
                        typeGrid[id] = voxel.type;
                        densityGrid[id] = voxel.density;
                    }
                }
            }

            var mesh = MeshBuilder.builder.GenerateMesh(densityGrid, typeGrid, arraySize, Vector3.zero, out int[] blocktypes);

            if (mesh.triangles.Length > 0) {

                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;

                Material[] materials = new Material[blocktypes.Length];
                for (int b = 0; b < blocktypes.Length; b++) {
                    materials[b] = SNContentLoader.GetMaterialForType(blocktypes[b]);
                }
                gameObject.GetComponent<MeshRenderer>().materials = materials;
            }
        }
    }
}