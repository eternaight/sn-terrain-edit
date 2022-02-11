using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public static class VoxelOps {

        public static void VoxelAddSmooth(VoxelData voxelData, float distanceIncrement, byte newSolidVoxelType) {
            // encode density into addition-friendly format
            float distanceValue = Mathf.Max(voxelData.signedDistance, distanceIncrement);
            
            byte blocktype = 0;
            if (distanceValue > 0) {
                blocktype = newSolidVoxelType;
            }

            voxelData.blocktype = blocktype;
            voxelData.signedDistance = distanceValue;
        }

        public static void VoxelPaint(VoxelData source, byte selectedBlocktype) {
            if (source.blocktype == 0) return; 
            source.blocktype = selectedBlocktype;
        }

        public static void VoxelFlatten(VoxelData source, Vector3Int pos, Vector3 planeOrigin, Vector3 planeNorm, byte newSolidType) {
            float planeDistance = -PlaneSDF(pos, planeOrigin, planeNorm);
            byte t = planeDistance > 0 ? newSolidType : (byte)0;
            source.blocktype = t;
            source.signedDistance = planeDistance;
        }

        public static void VoxelSmooth(VoxelData voxel, Vector3Int pos, byte newSolidType, int blurRadius) {

            // basically Gaussian blur
            bool solidBefore = voxel.signedDistance > 0;
            float sum = 0;
            int count = 0;

            for (int k = pos.z - blurRadius; k <= pos.z + blurRadius; k++) {
                for (int j = pos.y - blurRadius; j <= pos.y + blurRadius; j++) {
                    for (int i = pos.x - blurRadius; i <= pos.x + blurRadius; i++) {
                        var data = VoxelMetaspace.instance.GetOctnodeVoxel(new Vector3Int(i, j, k), 5);
                        if (data is null) {
                            continue;
                        }
                        sum += data.signedDistance;
                        count++;
                    }
                }
            }

            float avgDistance = sum / count;
            voxel.blocktype = (avgDistance > 0 && !solidBefore) ? newSolidType : voxel.blocktype;
            voxel.signedDistance = avgDistance;
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