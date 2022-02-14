using UnityEngine;

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
            transform.position = Vector3.zero;// (Vector3)index * realSize;

            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshCollider>();

            MeshBounds = new Bounds(Vector3.one * realSize / 2f, Vector3.one * realSize);
        }

        public void ReloadMesh(int maxOctreeHeight, int biggestNode) {

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var mesh = MeshBuilderV2.instance.GenerateMesh(VoxelMetaspace.instance.GetOctnode(index), out int[] blocktypes);

            if (mesh.triangles.Length > 0) {
                
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;

                Material[] materials = new Material[blocktypes.Length];
                for (int blocktype = 0; blocktype < blocktypes.Length; blocktype++) {
                    materials[blocktype] = VoxelMetaspace.instance.GetMaterialForBlocktype(blocktypes[blocktype]);
                }
                gameObject.GetComponent<MeshRenderer>().materials = materials;
            } else {
                Debug.LogWarning("Empty mesh!");
            }

            sw.Stop();
        }
    }
}