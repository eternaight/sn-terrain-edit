using System;
using ReefEditor.Octrees;
using UnityEngine;

namespace ReefEditor.VoxelTech {
    public class VoxelGrid {
        public byte[] densityGrid;
        public byte[] typeGrid;
        byte[] oldDensityGrid;
        byte[] oldTypeGrid;
        public Vector3Int fullGridDim;
        public Vector3Int octreeIndex;
        public Vector3Int batchIndex;
        bool[] neighbourMap;

        public VoxelGrid(byte[] _coreDensity, byte[] _coreTypes, Vector3Int _octreeIndex, Vector3Int _batchIndex) {
            
            int _fullSide = VoxelWorld.RESOLUTION + 2;
            int leng = _fullSide * _fullSide * _fullSide;
            densityGrid = new byte[leng];
            typeGrid = new byte[leng];
            oldDensityGrid = new byte[_fullSide * _fullSide * _fullSide];
            oldTypeGrid = new byte[_fullSide * _fullSide * _fullSide];
            
            const int so = 1;

            for (int z = 0; z < VoxelWorld.RESOLUTION; z++) {
                for (int y = 0; y < VoxelWorld.RESOLUTION; y++) {
                    for (int x = 0; x < VoxelWorld.RESOLUTION; x++) {
                        SetVoxel(densityGrid, x + so, y + so, z + so, GetCoreVoxel(_coreDensity, x, y, z));
                        SetVoxel(typeGrid, x + so, y + so, z + so, GetCoreVoxel(_coreTypes, x, y, z));
                    }
                }
            }

            fullGridDim = Vector3Int.one * _fullSide;

            octreeIndex = _octreeIndex;
            batchIndex = _batchIndex;
        }

        public static byte GetVoxel(byte[] array, int x, int y, int z) {
            return array[Globals.LinearIndex(x, y, z, VoxelWorld.RESOLUTION + 2)];
        }
        public static void SetVoxel(byte[] array, int x, int y, int z, byte val) {
            array[Globals.LinearIndex(x, y, z, VoxelWorld.RESOLUTION + 2)] = val;
        }
        public static byte GetCoreVoxel(byte[] array, int x, int y, int z) {
            return array[Globals.LinearIndex(x, y, z, VoxelWorld.RESOLUTION)];
        }

        public void UpdateFullGrid() {

            int _fullSide = VoxelWorld.RESOLUTION + 2;
            neighbourMap = new bool[27];
            neighbourMap[13] = true;
            for (int z = 0; z < _fullSide; z++) {
                for (int y = 0; y < _fullSide; y++) {
                    for (int x = 0; x < _fullSide; x++) {
                        if (NeighbourOffsetFromVoxel(x, y, z) == Vector3Int.zero) {
                            continue;
                        }
                        VoxelGrid neigGrid;
                        Vector3Int neigOffset = NeighbourOffsetFromVoxel(x, y, z);
                        Vector3Int neigOctreeIndex = octreeIndex + neigOffset;
                        if (!VoxelMetaspace.OctreeExists(neigOctreeIndex, batchIndex)) {
                            Vector3Int neigBatchIndex = batchIndex + neigOffset;
                            if (!VoxelMetaspace.BatchExists(neigBatchIndex)) {
                                continue;
                            }
                            else {
                                // Get grid from neighbouring VoxelMesh
                                neigGrid = VoxelMetaspace.metaspace.GetVoxelGrid(neigBatchIndex, IndexMod(neigOctreeIndex, 5));
                                neighbourMap[(neigOffset.x + 1) + (neigOffset.y + 1) * 3 + (neigOffset.z + 1) * 9] = true;
                            }
                        } else {
                            // Get grid from neighbouring container
                            neigGrid = VoxelMetaspace.metaspace.GetVoxelGrid(batchIndex, neigOctreeIndex);
                            neighbourMap[(neigOffset.x + 1) + (neigOffset.y + 1) * 3 + (neigOffset.z + 1) * 9] = true;
                        }

                        Vector3Int sample = new Vector3Int(x, y, z);
                        if (x == 0) sample.x = VoxelWorld.RESOLUTION;
                        else if (x == VoxelWorld.RESOLUTION + 1) sample.x = 1;
                        
                        if (y == 0) sample.y = VoxelWorld.RESOLUTION;
                        else if (y == VoxelWorld.RESOLUTION + 1) sample.y = 1;

                        if (z == 0) sample.z = VoxelWorld.RESOLUTION;
                        else if (z == VoxelWorld.RESOLUTION + 1) sample.z = 1;

                        SetVoxel(densityGrid, x, y, z, GetVoxel(neigGrid.densityGrid, sample.x, sample.y, sample.z));
                        SetVoxel(typeGrid, x, y, z, GetVoxel(neigGrid.typeGrid, sample.x, sample.y, sample.z));
                    }
                }
            }
        }

        Vector3Int NeighbourOffsetFromVoxel(int x, int y, int z) {
            Vector3Int offset = Vector3Int.zero;
            if (x <= 0) offset.x = -1;
            else if (x >= VoxelWorld.RESOLUTION + 1) offset.x = 1;

            if (y <= 0) offset.y = -1;
            else if (y >= VoxelWorld.RESOLUTION + 1) offset.y = 1;

            if (z <= 0) offset.z = -1;
            else if (z >= VoxelWorld.RESOLUTION + 1) offset.z = 1;

            return offset;
        }
        Vector3Int IndexMod(Vector3Int octreeIndex, int mod) => new Vector3Int((octreeIndex.x + mod) % mod, (octreeIndex.y + mod) % mod, (octreeIndex.z + mod) % mod);
        public void GetFullGrids(out byte[] _fullDensityGrid, out byte[] _fullTypeGrid) {
            _fullDensityGrid =   densityGrid;
            _fullTypeGrid =      typeGrid;
        }

        public void Cache() {
            Array.Copy(densityGrid, oldDensityGrid, densityGrid.Length);
            Array.Copy(typeGrid, oldTypeGrid, typeGrid.Length);
        }
        public byte[] GetCachedVoxel(int x, int y, int z) => new byte[] { GetVoxel(oldDensityGrid, x, y, z), GetVoxel(oldTypeGrid, x, y, z) };

        public void ApplyDensityFunction(Brush.BrushStroke stroke, Vector3 gridOrigin) {
            const int offset = 1;
            int fullSide = VoxelWorld.RESOLUTION + 2 - offset;
            for (int z = offset; z < fullSide; z++) {
                for (int y = offset; y < fullSide; y++) {
                    for (int x = offset; x < fullSide; x++) {
                        Vector3Int neigOffset = NeighbourOffsetFromVoxel(x, y, z);
                        if (neighbourMap[(neigOffset.x + 1) + (neigOffset.y + 1) * 3 + (neigOffset.z + 1) * 9]) {
                            ApplyGridAction(x, y, z, gridOrigin, stroke);
                        }
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
            int distanceValue = GetVoxel(densityGrid, x, y, z);
            if (distanceValue == 0) {
                distanceValue = GetVoxel(typeGrid, x, y, z) == 0 ? 0 : 252;
            }

            // run with it
            bool solidChanged = (distanceValue >= 126) != (distanceValue + increment >= 126);
            distanceValue += increment;

            // decode density back into normal storage format
            if (distanceValue >= 252 || distanceValue <= 0) {
                // 'far' node
                SetVoxel(densityGrid, x, y, z, 0);
            } else {
                SetVoxel(densityGrid, x, y, z, (byte)distanceValue);
            }

            if (solidChanged) {
                if (distanceValue >= 126) {
                    SetVoxel(typeGrid, x, y, z, newSolidVoxelType);
                } else {
                    SetVoxel(typeGrid, x, y, z, 0);
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

            if (functionDensity > 0 && GetVoxel(densityGrid, x, y, z) > 126) {
                SetVoxel(typeGrid, x, y, z, Brush.selectedType);
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
            bool solidBefore = GetVoxel(oldDensityGrid, x, y, z) >= 126;
            int sum = 0;
            int count = (int)Mathf.Pow((1 + blurRadius * 2), 3) - 1;

            for (int k = z - blurRadius; k <= z + blurRadius; k++) {
                for (int j = y - blurRadius; j <= y + blurRadius; j++) {
                    for (int i = x - blurRadius; i <= x + blurRadius; i++) {
                        if (i != x || j != y || k != z) {

                            Vector3Int voxel = new Vector3Int(i, j, k), octree = octreeIndex, batch = batchIndex;
                            if (!VoxelMetaspace.VoxelExists(i, j, k)) {
                                // its in another octree
                                Vector3Int neigOffset = NeighbourOffsetFromVoxel(i, j, k);
                                voxel = IndexMod(new Vector3Int(i, j, k) + neigOffset * 2, VoxelWorld.RESOLUTION + 2);
                                octree += neigOffset;
                                if (!VoxelMetaspace.OctreeExists(octree, batchIndex)) {
                                    // its in another batch
                                    octree = IndexMod(octree, 5);
                                    batch += neigOffset;
                                    if (!VoxelMetaspace.BatchExists(batch)) {
                                        count--;
                                        continue;
                                    }
                                }
                            }

                            byte[] voxelData = VoxelMetaspace.metaspace.GetCachedVoxel(voxel, octree, batch);
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
            SetVoxel(densityGrid, x, y, z, (byte)(sum));

            // update type as well
            bool solidNow = GetVoxel(densityGrid, x, y, z) >= 126;
            if (solidNow != solidBefore) {
                if (solidNow) {
                    SetVoxel(typeGrid, x, y, z, Brush.selectedType);
                } else {
                    SetVoxel(typeGrid, x, y, z, 0);
                }
            }
        }
        void DensityAction_Flatten(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // Make voxels solid below surface & inside brush
            // Make voxels empty above surface & inside brush

            // offset sample position because full grid
            float planeDensity = SampleDensity_Plane(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.firstStrokePoint, stroke.firstStrokeNormal);
            float sphereDensity = SampleDensity_Sphere(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedDensity = Mathf.Clamp(planeDensity, -1, 1);
            byte encodedDensity = OctNodeData.EncodeDensity(clampedDensity);

            // only affect voxels inside spherical brush
            if (sphereDensity > 0) {
                if (planeDensity != clampedDensity) {
                    SetVoxel(densityGrid, x, y, z, 0); // far node
                } else {
                    SetVoxel(densityGrid, x, y, z, encodedDensity);
                }

                if (encodedDensity >= 126) {
                    SetVoxel(typeGrid, x, y, z, Brush.selectedType);
                } else {
                    SetVoxel(typeGrid, x, y, z, 0); // solid node
                }
            }
        }
    }
}