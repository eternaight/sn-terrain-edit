using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

public class Batch : MonoBehaviour
{
    public bool drawn;
    public int octreeSize;
    public Vector3Int batchIndex;
    public Octree[,,] rootNodes;

    // other objects
    VoxelandMesh _voxeland;

    public event Action OnBatchConstructed;

    public void Setup() {

        drawn = false;
        octreeSize = 32;
        _voxeland = FindObjectOfType<VoxelandMesh>();

        transform.position = (batchIndex - RegionLoader.loader.start) * octreeSize * 5;

        BoxCollider coll = gameObject.AddComponent<BoxCollider>();
        gameObject.layer = 1;

        coll.center = new Vector3(octreeSize * 2.5f, octreeSize * 2.5f, octreeSize * 2.5f);
        coll.size = new Vector3(octreeSize * 5, octreeSize * 5, octreeSize * 5);
        coll.isTrigger = true;
        
        ReadBatch();
    }

    public void ReadBatch() {
        BatchReadWriter.readWriter.ReadBatch(this);
    }

    public void StartConstructingBatch_MatGallery() {
        BatchReadWriter.readWriter.DoMatGalleryBatch(this);

        BoxCollider coll = gameObject.AddComponent<BoxCollider>();
        coll.center = new Vector3(octreeSize * 2.5f, octreeSize * 2.5f, octreeSize * 2.5f);
        coll.size = new Vector3(octreeSize * 5, octreeSize * 5, octreeSize * 5);
        coll.isTrigger = true;
    }

    public void ConstructBatch() {
        
        _voxeland.Init(rootNodes);
        OnBatchConstructed();
    }

    public void Write() {
        BatchReadWriter.readWriter.WriteBatch(batchIndex, rootNodes);
    }

    public OctNodeData GetNode(Vector3 pos) {
        Vector3 localPos = pos - rootNodes[0, 0, 0].Origin;
        int x = Mathf.Clamp((int)(localPos.x / 32f), 0, 4);
        int y = Mathf.Clamp((int)(localPos.y / 32f), 0, 4);
        int z = Mathf.Clamp((int)(localPos.z / 32f), 0, 4);

        if (z < 5 && y < 5 && x < 5) {
            return rootNodes[z, y, x].GetNodeFromPos(pos);
        }
        else {
            Debug.LogError($"Out of bounds density request! ({x}, {y}, {z})");
            return new OctNodeData(false);
        }
    }

    public void SetOctrees(Octree[,,] octrees) {
        rootNodes = octrees;
    }
}