using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BatchReadWriter : MonoBehaviour
{
    public static BatchReadWriter readWriter;

    public bool busy = false;

    public delegate void ReadFinishedCall(Octree[,,] nodes); 

    void Awake() {
        readWriter = this;
    }

    public void ReadBatch(Batch batch) {
        
        if (!busy)
            StartCoroutine(DoReadBatch(batch.OnFinishRead, batch.batchIndex));
    }

    public void DoMatGalleryBatch(Batch batch) {
        
        GenerateMaterialGallery(batch);
    }

    public IEnumerator DoReadBatch(ReadFinishedCall readFinishedCall, Vector3Int batchIndex) {
        string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);
        busy = true;

        //Debug.Log("Reading "+ batchname);

        Octree[,,] octrees = new Octree[5, 5, 5];
        int countOctrees = 0;

        if (File.Exists(Globals.instance.batchSourcePath + batchname)) {

            BinaryReader reader = new BinaryReader(File.Open(Globals.instance.batchSourcePath + batchname, FileMode.Open));
            reader.ReadInt32();
            
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
                    byte type = data[curr_pos + 2 + i * 4];
                    byte signedDist = data[curr_pos + 3 + i * 4];
                    ushort childIndex = (ushort)(data[curr_pos + 5 + i * 4] * 256 + data[curr_pos + 4 + i * 4]);

                    nodesOfThisOctree[i] = (new OctNodeData((byte)type, (byte)signedDist, (ushort)childIndex));
                }

                Octree octree = new Octree(x, y, z, RegionLoader.octreeSize, batchIndex * 160);
                octree.Write(nodesOfThisOctree);
                octrees[z, y, x] = octree;

                curr_pos += (nodeCount * 4) + 2;
                countOctrees++; 
            }

            readFinishedCall(octrees);
            reader.Close();
        } 
        
        // if no batch file
        else {
            Debug.Log("no file for batch " + batchname);
            readFinishedCall(null);
        }

        busy = false;
        yield return null;
    } 

    public bool QuickReadBatch(Vector3Int batchIndex, out int[,,] octrees) {
        string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);
        busy = true;

        octrees = new int[5, 5, 5];

        if (File.Exists(Globals.instance.batchSourcePath + batchname)) {

            BinaryReader reader = new BinaryReader(File.Open(Globals.instance.batchSourcePath + batchname, FileMode.Open));
            reader.ReadInt32();
            
            // assemble a data array
            int curr_pos = 0;
            int countOctrees = 0;
            
            long streamLength = reader.BaseStream.Length - 4;
            while (curr_pos < streamLength) {
                
                int x = countOctrees / 25;
                int y = countOctrees % 25 / 5;
                int z = countOctrees % 5;

                int nodeCount = reader.ReadByte() + reader.ReadByte() * 256;

                octrees[z, y, x] = reader.ReadByte();

                byte[] buffer = new byte[3 + (nodeCount - 1) * 4];
                reader.Read(buffer, 0, 3 + (nodeCount - 1) * 4);

                curr_pos += (nodeCount * 4) + 2;
                countOctrees++; 

                curr_pos++;
            }

            reader.Close();
            busy = false;
            return true;
        } 
        busy = false;
        return false;
    }

    public void GenerateMaterialGallery(Batch batch) {

        Octree[,,] octrees = new Octree[5, 5, 5];

        byte label = RegionLoader.loader.GetLabel(batch.batchIndex);

        // form base
        for (int z = 0; z < 5; z++) {
            for (int x = 0; x < 5; x++) {

                List<OctNodeData> nodes = new List<OctNodeData>();

                nodes.Add(new OctNodeData(37, 0, 0));

                Octree octree = new Octree(x, 0, z, RegionLoader.octreeSize, batch.transform.position);
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

                Octree octree = new Octree(x, 1, z, RegionLoader.octreeSize, batch.transform.position);
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

                    Octree octree = new Octree(x, y, z, RegionLoader.octreeSize, batch.transform.position);
                    octree.Write(nodes.ToArray());
                    octrees[z, y, x] = octree;

                } 
            }
        }

        busy = false;
        batch.OnFinishRead(octrees);
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

    public bool WriteOptoctrees(Vector3 batchIndex, Octree[,,] octrees) { 
        string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);
        busy = true;
        
        Debug.Log($"Writing {batchname} to {Globals.instance.batchOutputPath}");

        BinaryWriter writer = new BinaryWriter(File.Open(Globals.instance.batchOutputPath + batchname, FileMode.OpenOrCreate));
        writer.Write(4);
            
        for (int z = 0; z < 5; z++) {
            for (int y = 0; y < 5; y++) {
                for (int x = 0; x < 5; x++) {
                    WriteOctree(writer, octrees[x, y, z]);
                }
            }
        }

        writer.Close();
        busy = false;
        return true;
    }

    public bool WriteEsperPatch(Vector3 batchIndex, Octree[,,] nodes, Octree[,,] originalNodes) {
        string batchname = string.Format("\\batch-{0}-{1}-{2}.esperpatch", batchIndex.x, batchIndex.y, batchIndex.z);
        busy = true;
        
        Debug.Log($"Writing {batchname} to {Globals.instance.batchOutputPath}");

        BinaryWriter writer = new BinaryWriter(File.Open(Globals.instance.batchOutputPath + batchname, FileMode.OpenOrCreate));
        writer.Write(0u);

        // get changed octrees data
        List<Octree> batchChanges = new List<Octree>();
        for (int z = 0; z < 5; z++) {
            for (int y = 0; y < 5; y++) {
                for (int x = 0; x < 5; x++) {
                    if (nodes[x, y, z].IdenticalTo(originalNodes[x, y, z])) {
                        batchChanges.Add(nodes[x, y, z]);
                    }
                } 
            }
        }
        
        // start of batch write
        writer.Write((short)batchIndex.x);
        writer.Write((short)batchIndex.y);
        writer.Write((short)batchIndex.z);

        // num octrees to replace
        writer.Write((byte)batchChanges.Count);

        foreach (Octree change in batchChanges) {
            writer.Write(change.Index);
            WriteOctree(writer, change);
        }

        writer.Close();
        busy = false;
        return true;
    }

    void WriteOctree(BinaryWriter writer, Octree octree) {
        //assemble the octnode array
        OctNodeData[] nodes = octree.Read();

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