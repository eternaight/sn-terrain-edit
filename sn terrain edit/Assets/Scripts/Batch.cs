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
    int lod;

    VoxelMesh _voxelMesh;

    public event Action OnBatchConstructed;

    public void Setup(int _lod) {

        drawn = false;
        octreeSize = 32;
        _voxelMesh = gameObject.AddComponent<VoxelMesh>();
        lod = _lod;

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

    public void OnFinishRead(Octree[,,] octrees) {
        rootNodes = octrees;
        if (rootNodes != null) {
            _voxelMesh.Init(rootNodes, lod);
        }
        OnBatchConstructed();
    }

    public void Write() {
        _voxelMesh.UpdateOctreeDensity();
        BatchReadWriter.readWriter.WriteOptoctrees(batchIndex, rootNodes);
    }

    public OctNodeData GetNode(Vector3 pos) {
        Vector3 localPos = pos - rootNodes[0, 0, 0].Origin;
        int x = (int)(localPos.x / 32f);
        int y = (int)(localPos.y / 32f);
        int z = (int)(localPos.z / 32f);

        int _cps = VoxelMesh.CONTAINERS_PER_SIDE;
        if (z < _cps && y < _cps && x < _cps && x >= 0 && y >= 0 && z >= 0) {
            return rootNodes[z, y, x].GetNodeFromPos(pos);
        }
        else {
            Debug.LogError($"Out of bounds density request! ({x}, {y}, {z})");
            return new OctNodeData(false);
        }
    }
}