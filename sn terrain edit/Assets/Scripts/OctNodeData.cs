using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor {
    public class OctNodeData {

        public byte type;
        public byte density;
        public ushort childPosition;

        public OctNodeData() {
            type = 0;
            density = 0;
            childPosition = 0;
        }
        public OctNodeData(byte type, byte density) {
            this.type = type;
            this.density = density;
            childPosition = 0;
        }
        public OctNodeData(byte type, byte density, ushort childPos) {
            this.type = type;
            this.density = density;
            childPosition = childPos;
        }
        public OctNodeData(OctNodeData other) {
            type = other.type;
            density = other.density;
            childPosition = other.childPosition;
        }

        public bool IsBelowSurface() {
            if (density == 0) {
                return type > 0;
            }
            return density >= 126;
        }
        public static bool IsBelowSurface(byte type, byte signedDist) {
            if (signedDist == 0) {
                return type > 0;
            }
            return signedDist >= 126;
        }

        public static float DecodeDensity(byte densityByte) {
            return (densityByte - 126) / 126f;
        }
        public static byte EncodeDensity(float distanceValue) {
            return (byte)(Mathf.Clamp(distanceValue, -1, 1) * 126 + 126);
        }

        public override string ToString() {
            return $"OctNodeData(t: {type}, d: {density}, c: {childPosition})";
        }

        public override bool Equals(object obj) {
            if (obj is OctNodeData data) {
                return (
                    data.type == type &&
                    data.density == density &&
                    data.childPosition == childPosition
                );
            }
            return false;
        }

        public override int GetHashCode() {
            return (childPosition * 31 + type) * 31 + density;
        }

        public static bool operator ==(OctNodeData one, OctNodeData other) {
            return (
                other.type == one.type &&
                other.density == one.density &&
                other.childPosition == one.childPosition
            );
        }
        public static bool operator !=(OctNodeData one, OctNodeData other) {
            return (
                other.type != one.type ||
                other.density != one.density ||
                other.childPosition != one.childPosition
            );
        }
    }
}
