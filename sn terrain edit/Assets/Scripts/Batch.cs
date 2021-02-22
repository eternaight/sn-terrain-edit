﻿using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

public class Batch : MonoBehaviour
{
    public bool drawn;
    public int octreeSize;
    public Vector3Int batchIndex;
    public Octree[,,] rootNodes;

    VoxelMesh _voxelMesh;

    public byte mainBlockType;

    public event Action OnBatchConstructed;

    public void Setup() {

        drawn = false;
        octreeSize = 32;
        _voxelMesh = FindObjectOfType<VoxelMesh>();

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
        
        _voxelMesh.Init(rootNodes);
        byte res = 0;
        for (int i = 0; i < 125 && res == 0; i++) {
            res = rootNodes[i, (i % 25) / 5, i / 25].GetBlockType();
        }
        mainBlockType = res;
        Globals.SetMatBlockType(mainBlockType);
        Globals.get.UpdateBatchMaterial(false);
        OnBatchConstructed();
    }

    public void Write() {
        _voxelMesh.UpdateOctreeDensity();
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