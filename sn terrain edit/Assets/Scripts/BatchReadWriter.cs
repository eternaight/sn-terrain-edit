using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ReefEditor.VoxelEditing;
using ReefEditor.UI;

namespace ReefEditor {
    public static class BatchReadWriter {

        public static IEnumerator<OctNode> ReadBatch(Vector3Int batchIndex) {
            string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);

            Vector3Int octreeDimensions = Vector3Int.one * 5;
            if (batchIndex.x == 25) octreeDimensions.x = 3;
            if (batchIndex.z == 25) octreeDimensions.z = 3;

            int countOctrees = 0;
            int expectedOctrees = octreeDimensions.x * octreeDimensions.y * octreeDimensions.z;

            if (File.Exists(EditorManager.instance.batchSourcePath + batchname)) {

                var reader = new BinaryReader(File.Open(EditorManager.instance.batchSourcePath + batchname, FileMode.Open));
                reader.ReadInt32();
                
                // assemble a data array
                int curr_pos = 0;
                
                long streamLength = reader.BaseStream.Length - 4;
                byte[] data = new byte[streamLength];
                while (curr_pos < streamLength) {

                    data[curr_pos] = reader.ReadByte();
                    curr_pos++;
                }

                curr_pos = 0;
                while (curr_pos < data.Length && countOctrees < expectedOctrees) {
                    
                    int x = countOctrees / (octreeDimensions.z * octreeDimensions.y);
                    int y = countOctrees % (octreeDimensions.z * octreeDimensions.y) / octreeDimensions.z;
                    int z = countOctrees % octreeDimensions.z;

                    int nodeCount = data[curr_pos + 1] * 256 + data[curr_pos];
                    // record all nodes of this octree in an array
                    OctNodeData[] nodesOfThisOctree = new OctNodeData[nodeCount];
                    for (int i = 0; i < nodeCount; ++i) {
                        byte type = data[curr_pos + 2 + i * 4];
                        byte signedDist = data[curr_pos + 3 + i * 4];
                        ushort childIndex = (ushort)(data[curr_pos + 5 + i * 4] * 256 + data[curr_pos + 4 + i * 4]);

                        nodesOfThisOctree[i] = (new OctNodeData(type, signedDist, childIndex));
                    }

                    var node = new OctNode(batchIndex * 160 + new Vector3Int(x, y, z) * 32, 32);
                    node.ReadArray(nodesOfThisOctree, 0);
                    yield return node;

                    curr_pos += (nodeCount * 4) + 2;
                    countOctrees++; 
                }

                reader.Close();
            } 
            
            // if no batch file
            else {
                Debug.Log("no file for batch " + batchname);
                // TODO: remove dependency
                EditorUI.DisplayErrorMessage(   $"No file for batch {batchIndex.x}-{batchIndex.y}-{batchIndex.z}\n" +
                                                $"Created an empty batch", EditorUI.NotificationType.Warning);
                for (int i = 0; i < 125; i++)
                {
                    var node = new OctNode(batchIndex * 160 + new Vector3Int(i / 25, i % 25 / 5, i % 5) * 32, 32);
                    node.ReadArray(new OctNodeData[] { new OctNodeData(0, 0, 0) }, 0);
                    yield return node;
                }
            }
        } 

        public static IEnumerator WriteOptOctreesCoroutine(VoxelMetaspace metaspace) {
            foreach (var batchId in metaspace.BatchIndices()) {
                var nodes = metaspace.OctreesOfBatch(batchId);
                WriteOptoctrees(batchId, nodes);
                yield return null;
            }
        } 
        private static bool WriteOptoctrees(Vector3 batchIndex, IEnumerator<OctNode> rootNodes) { 
            string batchname = string.Format("\\compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);
            
            Debug.Log($"Writing {batchname} to {EditorManager.instance.batchOutputPath}");

            BinaryWriter writer = new BinaryWriter(File.Open(EditorManager.instance.batchOutputPath + batchname, FileMode.OpenOrCreate));
            writer.Write(4);
            
            while (rootNodes.MoveNext()) { 
                WriteOctree(writer, rootNodes.Current);
            }

            writer.Close();
            return true;
        }
        
        public static IEnumerator WriteOctreePatchCoroutine(VoxelMetaspace metaspace) {
            Debug.Log($"Writing world patch as {EditorManager.instance.batchOutputPath}");

            BinaryWriter writer = new BinaryWriter(File.Open(EditorManager.instance.batchOutputPath, FileMode.Create));
            // write version
            writer.Write(0u);
    	    
            foreach (var batchId in metaspace.BatchIndices()) {
                var modifiedNodes = metaspace.OctreesOfBatch(batchId);
                // load original nodes from file?
                var originalNodes = ReadBatch(batchId);

                // get changed octrees data
                var batchChanges = new List<OctNode>();
                while (originalNodes.MoveNext() & modifiedNodes.MoveNext()) {
                    if (!modifiedNodes.Current.IdenticalTo(originalNodes.Current)) {
                        batchChanges.Add(modifiedNodes.Current);
                    }
                }
                
                Debug.Log($"Patch contains {batchChanges.Count} changed octrees.");
                if (batchChanges.Count != 0) {
                    // start of batch write
                    writer.Write((short)batchId.x);
                    writer.Write((short)batchId.y);
                    writer.Write((short)batchId.z);

                    // num octrees to replace
                    writer.Write((byte)batchChanges.Count);

                    foreach (var change in batchChanges) {
                        byte index = change.GetXMajorLocalOctreeIndex();
                        if (index > 125) Debug.Log("found an octree index > 125");
                        writer.Write(index);
                        WriteOctree(writer, change);
                    }
                }

                yield return null;
            }

            writer.Close();
        }

        private static void WriteOctree(BinaryWriter writer, OctNode root) {
            //assemble the octnode array
            var nodes = new List<OctNodeData>();
            root.WriteToArray(nodes);

            // write number of nodes in this octree
            ushort numNodes = (ushort)nodes.Count;
            writer.Write(numNodes);

            // write type, signedDist, childIndex of each octree
            foreach (OctNodeData data in nodes) {
                writer.Write(data.type);
                writer.Write(data.density);
                writer.Write(data.childPosition);
            }
        }
    }
}
