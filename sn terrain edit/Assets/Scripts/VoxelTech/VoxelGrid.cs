using ReefEditor.Octrees;
using UnityEngine;

namespace ReefEditor.VoxelTech {
    public class VoxelGrid {
        private readonly byte[] densityGrid;
        private readonly byte[] typeGrid;

        public readonly Vector3Int arraySize;
        public Vector3Int worldOriginVoxel;
        public Vector3Int octreeOrigin;

        public VoxelGrid(Vector3Int index, int resolution) {

            int fullResolution = resolution + 2;
            arraySize = Vector3Int.one * fullResolution;

            const int coreOffset = 1;
            octreeOrigin = index * resolution;
            worldOriginVoxel = octreeOrigin - Vector3Int.one * coreOffset;

            int leng = fullResolution * fullResolution * fullResolution;
            densityGrid = new byte[leng];
            typeGrid = new byte[leng];
        }

        public byte GetVoxelBlocktype(Vector3Int voxel) {
            return typeGrid[Globals.LinearIndex(voxel - worldOriginVoxel, arraySize)];
        }
        public byte GetVoxelDensity(Vector3Int voxel) {
            return densityGrid[Globals.LinearIndex(voxel - worldOriginVoxel, arraySize)];
        }
        public byte[] GetVoxel(Vector3Int voxel) {
            return new byte[] { GetVoxelBlocktype(voxel), GetVoxelDensity(voxel) };
        }
        public void SetVoxel(Vector3Int voxel, byte[] data) {
            var id = Globals.LinearIndex(voxel - worldOriginVoxel, arraySize);
            typeGrid[id] = data[0];
            densityGrid[id] = data[1];
        }

        public void UpdateFullGrid() {

            for (int z = 0; z < arraySize.z; z++) {
                for (int y = 0; y < arraySize.y; y++) {
                    for (int x = 0; x < arraySize.x; x++) {
                        var voxel = VoxelMetaspace.metaspace.GetVoxelFromOctree(worldOriginVoxel + new Vector3Int(x, y, z));

                        var id = Globals.LinearIndex(x, y, z, arraySize);
                        typeGrid[id] = voxel.type;
                        densityGrid[id] = voxel.density;
                    }
                }
            }
        }

        public Mesh GenerateMesh(out int[] blocktypes) {
            return MeshBuilder.builder.GenerateMesh(densityGrid, typeGrid, arraySize, Vector3.zero, out blocktypes);
        }

        public void BlendGrids(Brush.BrushStroke stroke, IVoxelGrid grid) {
            for (int z = 0; z < arraySize.x; z++) {
                for (int y = 0; y < arraySize.y; y++) {
                    for (int x = 0; x < arraySize.z; x++) {

                        // this is just to mask voxels outside world bounds
                        if (VoxelMetaspace.metaspace.GetGridForVoxel(worldOriginVoxel + new Vector3Int(x, y, z)) is null) continue;
                        if (!grid.GetMask(x, y, z)) continue;

                        ApplyGridAction(x, y, z, worldOriginVoxel, stroke);
                    }
                }
            }
        }

        public void ApplyGridAction(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke brushStroke) {
            switch (brushStroke.brushMode) {
                case BrushMode.Add:
                    DensityAction_AddSmooth(x, y, z, gridOrigin, brushStroke);
                    break;
                case BrushMode.Remove:
                    DensityAction_AddSmooth(x, y, z, gridOrigin, brushStroke, true);
                    break;
                case BrushMode.Paint:
                    DensityAction_Paint(x, y, z, gridOrigin, brushStroke);
                    break;
                case BrushMode.Smooth:
                    DensityAction_Smooth(x, y, z, gridOrigin, brushStroke);
                    break;
                case BrushMode.Flatten:
                    DensityAction_Flatten(x, y, z, gridOrigin, brushStroke);
                    break;
                default:
                    break;
            }
        }
        void VoxelAdd(int x, int y, int z, int increment, byte newSolidVoxelType) {
            // encode density into addition-friendly format
            int distanceValue = 0;// GetDensity(x, y, z);
            if (distanceValue == 0) {
                distanceValue = 0;// GetType(x, y, z) == 0 ? 0 : 252;
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
        }

        public static float SampleDensity_Sphere(Vector3 sample, Vector3 origin, float radius) {
            return radius - (sample - origin).magnitude;
        }
        public static float SampleDensity_Plane(Vector3 sample, Vector3 origin, Vector3 normal) {
            float d = -(origin.x * normal.x + origin.y * normal.y + origin.z * normal.z);
            return -(sample.x * normal.x + sample.y * normal.y + sample.z * normal.z + d);
        }

        void DensityAction_AddSmooth(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke, bool remove = false) {
            // offset sample position because full grid
            float functionDensity = SampleDensity_Sphere(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedFunctionDensity = Mathf.Clamp(functionDensity, -1, 1);
            byte encodedDensity = OctNodeData.EncodeDensity(clampedFunctionDensity);

            if (clampedFunctionDensity > 0) {

                float add = clampedFunctionDensity * stroke.GetWeight(new Vector3(x, y, z) + gridOrigin);
                if (remove) add *= -1;
                VoxelAdd(x, y, z, (int)add, (byte)Brush.selectedType);
            }
        }
        void DensityAction_Paint(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // Paint voxels on the intersection of mesh and brush
            // offset sample position because full grid
            float functionDensity = SampleDensity_Sphere(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedFunctionDensity = Mathf.Clamp(functionDensity, -1, 1);

            var density = 125;// GetDensity(x, y, z);
            if (functionDensity > 0 && density > 126) {
                //SetVoxel(x, y, z, Brush.selectedType, density);
            }
        }
        void DensityAction_Smooth(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // basically Gaussian blur
            // If voxel is outside the brush, skip it
            // offset sample position because full grid
            if (SampleDensity_Sphere(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.brushLocation, stroke.brushRadius) < 0) {
                return;
            }

            const int blurRadius = 2;
            bool solidBefore = false;// GetDensity(x, y, z) >= 126;
            int sum = 0;
            int count = (int)Mathf.Pow((1 + blurRadius * 2), 3) - 1;

            for (int k = z - blurRadius; k <= z + blurRadius; k++) {
                for (int j = y - blurRadius; j <= y + blurRadius; j++) {
                    for (int i = x - blurRadius; i <= x + blurRadius; i++) {
                        if (i != x || j != y || k != z) {

                            var grid = VoxelMetaspace.metaspace.GetGridForVoxel(worldOriginVoxel + new Vector3Int(i, j, k));
                            if (grid is null) {
                                count--;
                                continue;
                            }

                            byte[] voxelData = new byte[2]; // VoxelMetaspace.metaspace.GetCachedVoxel(worldOriginVoxel + new Vector3Int(i, j, k));
                            if (voxelData[0] == 0 && voxelData[1] != 0) {
                                sum += 252;
                            } else {
                                sum += voxelData[0];
                            }
                        }
                    }
                }
            }

            sum /= count;
            byte density = (byte)(sum);
            byte type = 0;

            // update type as well
            bool solidNow = density >= 126;
            if (solidNow && !solidBefore) {
                type = Brush.selectedType;
            }
        }
        void DensityAction_Flatten(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // Make voxels solid below surface & inside brush
            // Make voxels empty above surface & inside brush

            // offset sample position because full grid
            float planeDensity = SampleDensity_Plane(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.firstStrokePoint, stroke.firstStrokeNormal);
            byte encodedDensity = OctNodeData.EncodeDensity(planeDensity);

            byte t = 0, d = 0;

            if (Mathf.Abs(planeDensity) < 1) {
                d = encodedDensity;
            }

            if (encodedDensity >= 126) {
                t = Brush.selectedType;
            }
        }
    }
}