using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BatchReadWriter : MonoBehaviour
{
    [Header("Settings")]
    public static BatchReadWriter readWriter;
    [SerializeField] Vector3Int batchIndex;
    [Range(0, 7)]
    public int maxRecursionDepth = 3;

    public bool busy = false;

    Batch _batch;

    void Awake() {
        readWriter = this;
    }

    public void ReadBatch(Batch newbatch) {
        _batch = newbatch;
        batchIndex = _batch.batchIndex;
        
        if (!busy)
            StartCoroutine(DoReadBatch());
    }

    public void DoMatGalleryBatch(Batch newbatch) {
        _batch = newbatch;
        batchIndex = _batch.batchIndex;
        
        GenerateMaterialGallery();
    }

    public IEnumerator DoReadBatch() {
        string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);
        busy = true;

        Debug.Log("Reading "+ batchname);

        Octree[,,] octrees = new Octree[5, 5, 5];
        int countOctrees = 0;

        bool batchdataExists = File.Exists(Globals.get.batchSourcePath + batchname);
        byte batchLabel = RegionLoader.loader.GetLabel(batchIndex);

        if (batchdataExists) {

            BinaryReader reader = new BinaryReader(File.Open(Globals.get.batchSourcePath + batchname, FileMode.Open));
            string version = $"Terrain Version: {reader.ReadInt32()}";
            yield return null;
            
            // assemble a data array
            int curr_pos = 0;
            byte[] data = new byte[reader.BaseStream.Length - 4];
            
            long streamLength = reader.BaseStream.Length - 4;
            while (curr_pos < streamLength) {

                data[curr_pos] = reader.ReadByte();
                curr_pos++;
            }

            curr_pos = 0;
            while (curr_pos < data.Length) {
                
                int x = countOctrees / 25;
                int y = countOctrees % 25 / 5;
                int z = countOctrees % 5;

                int nodeCount = data[curr_pos + 1] * 256 + data[curr_pos];
                // record all nodes of this octree in an array
                OctNodeData[] nodesOfThisOctree = new OctNodeData[nodeCount];
                for (int i = 0; i < nodeCount; ++i) {
                    int type = data[curr_pos + 2 + i * 4];
                    int signedDist = data[curr_pos + 3 + i * 4];
                    int childIndex = data[curr_pos + 5 + i * 4] * 256 + data[curr_pos + 4 + i * 4];

                    nodesOfThisOctree[i] = (new OctNodeData((byte)type, (byte)signedDist, (ushort)childIndex));
                }

                Octree octree = new Octree(x, y, z, RegionLoader.octreeSize, _batch.transform.position);
                octree.Write(nodesOfThisOctree);
                octrees[z, y, x] = octree;

                curr_pos += (nodeCount * 4) + 2;
                countOctrees++; 
                // yield return null;
            }

            _batch.SetOctrees(octrees);
            reader.Close();
        } 
        
        // if no batch file
        else {
            Debug.Log("no file for batch " + batchname);
            _batch.SetOctrees(null);
        }

        busy = false;
        _batch.ConstructBatch();
    } 

    public void GenerateMaterialGallery() {

        Octree[,,] octrees = new Octree[5, 5, 5];

        byte label = RegionLoader.loader.GetLabel(batchIndex);

        // form base
        for (int z = 0; z < 5; z++) {
            for (int x = 0; x < 5; x++) {

                List<OctNodeData> nodes = new List<OctNodeData>();

                nodes.Add(new OctNodeData(37, 0, 0));

                Octree octree = new Octree(x, 0, z, RegionLoader.octreeSize, _batch.transform.position);
                octree.Write(nodes.ToArray());
                octrees[z, 0, x] = octree;

            } 
        }

        byte type = 1;
        // material layer
        for (int z = 0; z < 5; z++) {
            for (int x = 0; x < 5; x++) {

                List<OctNodeData> nodes = new List<OctNodeData>();

                Vector3Int octreeIndex = new Vector3Int(x, 1, z);
                nodes.Add(new OctNodeData(0, 0, 0));

                MatGalleryAction(ref nodes, 0, octreeIndex, label, ref type, 0);

                Octree octree = new Octree(x, 1, z, RegionLoader.octreeSize, _batch.transform.position);
                octree.Write(nodes.ToArray());
                octrees[z, 1, x] = octree;

            } 
        }

        // empty nodes
        for (int y = 2; y < 5; y++) {
            for (int z = 0; z < 5; z++) {
                for (int x = 0; x < 5; x++) {

                    List<OctNodeData> nodes = new List<OctNodeData>();

                    Vector3Int octreeIndex = new Vector3Int(x, y, z);
                    nodes.Add(new OctNodeData(0, 0, 0));

                    Octree octree = new Octree(x, y, z, RegionLoader.octreeSize, _batch.transform.position);
                    octree.Write(nodes.ToArray());
                    octrees[z, y, x] = octree;

                } 
            }
        }

        _batch.SetOctrees(octrees);
        busy = false;
        _batch.ConstructBatch();
    }

    public void MatGalleryAction(ref List<OctNodeData> nodes, int node, Vector3Int octree, byte label, ref byte nextType, int depth) {

        if (depth < 1) {
            ushort childIndex = (ushort)nodes.Count;

            OctNodeData newdata = new OctNodeData(nodes[node].type, nodes[node].signedDist, childIndex);
            nodes[node] = newdata;

            for (int b = 0; b < 8; b++) {
                if (IsBottomNode(b)) {
                    nodes.Add(new OctNodeData(nextType++, 0, 0));
                } else {
                    nodes.Add(new OctNodeData(0, 0, 0));
                }
            }

            // once children are in place, work with them
            for (int b = 0; b < 8; b++) {

                if (IsBottomNode(b)) {
                    MatGalleryAction(ref nodes, childIndex + b, octree, label, ref nextType, depth + 1);
                }
            }
        }
    }

    bool IsBottomNode(int b) {
        return b < 2 || b == 4 || b == 5;
    }

    public bool WriteBatch(Vector3 batchIndex, Octree[,,] octrees) { 
        string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);
        busy = true;
        
        Debug.Log($"Writing {batchname} to {Globals.get.batchOutputPath}");

        BinaryWriter writer = new BinaryWriter(File.Open(Globals.get.batchOutputPath + batchname, FileMode.OpenOrCreate));
        writer.Write(4);
            
        for (int x = 0; x < 5; x++) {
            for (int y = 0; y < 5; y++) {
                for (int z = 0; z < 5; z++) {

                    //assemble the octnode array
                    OctNodeData[] nodes = octrees[z, y, x].Read();

                    // write number of nodes in this octree
                    ushort numNodes = (ushort)nodes.Length;
                    writer.Write(numNodes);

                    // write type, signedDist, childIndex of each octree
                    for (int i = 0; i < nodes.Length; i++) {
                        writer.Write(nodes[i].type);
                        writer.Write(nodes[i].signedDist);
                        writer.Write(nodes[i].childPosition);
                    }
                }
            }
        }

        writer.Close();
        busy = false;
        return true;
    }
}