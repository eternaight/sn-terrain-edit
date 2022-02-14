using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public class VoxelData {
        public byte blocktype;
        public float signedDistance;

        public bool Solid {
            get {
                return signedDistance > 0;
            }
        }

        public VoxelData() {
            blocktype = 0;
            signedDistance = -2;
        }
        public VoxelData(byte _blocktype, float _signedDistance) {
            this.blocktype = _blocktype;
            this.signedDistance = _signedDistance;
        }
        public VoxelData(OctNodeData source) {
            blocktype = source.type;
            signedDistance = DecodeDensity(source.type, source.density);
        }
        public OctNodeData Encode() {
            return new OctNodeData(blocktype, EncodeDensity(signedDistance));
        }

        public bool IsNearVoxel() {
            return Mathf.Abs(signedDistance) <= 1;
        }

        private static float DecodeDensity(byte type, byte densityByte) {
            if (densityByte == 0) return type == 0 ? -2 : 2f;
            return (densityByte - 126) / 126f;
        }
        private static byte EncodeDensity(float signedDistance) {
            return Mathf.Abs(signedDistance) >= 1 ? (byte)0 : (byte)(signedDistance * 126 + 126);
        }
    }
}