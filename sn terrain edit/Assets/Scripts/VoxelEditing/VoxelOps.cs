using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public static class VoxelOps {

        public static OctNodeData VoxelAddSmooth(OctNodeData voxelData, int increment, byte newSolidVoxelType) {
            // encode density into addition-friendly format
            int distanceValue = voxelData.density;
            if (distanceValue == 0) {
                distanceValue = voxelData.type == 0 ? 0 : 252;
            }

            // run with it
            bool solidChanged = (distanceValue >= 126) != (distanceValue + increment >= 126);
            distanceValue += increment;

            // decode density back into normal storage format
            byte density = 0, type = 0;
            if (distanceValue < 252 && distanceValue > 0) {
                density = (byte)distanceValue;
            }

            if (solidChanged) {
                if (distanceValue >= 126) {
                    type = newSolidVoxelType;
                }
            }

            return new OctNodeData(type, density);
        }

        public static OctNodeData VoxelPaint(OctNodeData voxel, byte type) {
            return new OctNodeData(type, voxel.density);
        }

        public static OctNodeData VoxelFlatten(Vector3Int pos, Vector3 planeOrigin, Vector3 planeNorm, byte newSolidType) {
            byte density = OctNodeData.EncodeDensity(PlaneSDF(pos, planeOrigin, planeNorm));
            byte t = density >= 126 ? newSolidType : (byte)0;
            return new OctNodeData(t, density);
        }

        public static OctNodeData VoxelSmooth(OctNodeData voxel, Vector3Int pos, byte newSolidType, int blurRadius) {

            // basically Gaussian blur
            bool solidBefore = voxel.density >= 126;
            int sum = 0, count = 0;

            for (int k = pos.z - blurRadius; k <= pos.z + blurRadius; k++) {
                for (int j = pos.y - blurRadius; j <= pos.y + blurRadius; j++) {
                    for (int i = pos.x - blurRadius; i <= pos.x + blurRadius; i++) {

                        var data = VoxelMetaspace.instance.GetOctnodeVoxel(new Vector3Int(i, j, k), 5);
                        if (data is null) {
                            continue;
                        }

                        if (data.density == 0 && data.type != 0) {
                            sum += 252;
                        } else {
                            sum += data.density;
                        }
                        count++;
                    }
                }
            }

            byte density = (byte)(sum / count);
            byte type = (density >= 126 && !solidBefore ? newSolidType : (byte)0);
            return new OctNodeData(type, density);
        }


        private static float SphereSDF(Vector3 sample, Vector3 origin, float radius) {
            return radius - (sample - origin).magnitude;
        }
        private static float PlaneSDF(Vector3 sample, Vector3 origin, Vector3 normal) {
            float d = -(origin.x * normal.x + origin.y * normal.y + origin.z * normal.z);
            return -(sample.x * normal.x + sample.y * normal.y + sample.z * normal.z + d);
        }
    }
}