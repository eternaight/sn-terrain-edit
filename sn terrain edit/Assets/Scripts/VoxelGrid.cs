using System;
using ReefEditor.Octrees;
using UnityEngine;

namespace ReefEditor.VoxelTech {
    public class VoxelGrid {
        public byte[,,] densityGrid;
        public byte[,,] typeGrid;
        byte[,,] oldDensityGrid;
        byte[,,] oldTypeGrid;
        public Vector3Int fullGridDim;

        public VoxelGrid(byte[,,] _coreDensity, byte[,,] _coreTypes) {
            
            int _fullSide = VoxelMesh.RESOLUTION + 2;
            densityGrid = new byte[_fullSide, _fullSide, _fullSide];
            typeGrid = new byte[_fullSide, _fullSide, _fullSide];;
            
            int so = 1;

            for (int z = 0; z < VoxelMesh.RESOLUTION; z++) {
                for (int y = 0; y < VoxelMesh.RESOLUTION; y++) {
                    for (int x = 0; x < VoxelMesh.RESOLUTION; x++) {
                        densityGrid[x + so, y + so, z + so] = _coreDensity[x, y, z];
                        typeGrid[x + so, y + so, z + so] = _coreTypes[x, y, z];
                    }
                }
            }

            fullGridDim = Vector3Int.one * _fullSide;
        }

        public void AddPaddingFull(VoxelMesh mesh, Vector3Int containerIndex) {

            int _fullSide = VoxelMesh.RESOLUTION + 2;
            for (int z = 0; z < _fullSide; z++) {
                for (int y = 0; y < _fullSide; y++) {
                    for (int x = 0; x < _fullSide; x++) {
                        
                        Vector3Int neigIndex = containerIndex + CoreGridFromVertex(x, y, z);
                        if (GridExists(neigIndex)) {
                            VoxelGrid neigGrid = mesh.octreeContainers[Globals.LinearIndex(neigIndex.x, neigIndex.y, neigIndex.z, VoxelMesh.CONTAINERS_PER_SIDE)].grid;

                            Vector3Int sample = new Vector3Int(x, y, z);
                            if (x == 0) sample.x = VoxelMesh.RESOLUTION;
                            else if (x == VoxelMesh.RESOLUTION + 1) sample.x = 1;
                            
                            if (y == 0) sample.y = VoxelMesh.RESOLUTION;
                            else if (y == VoxelMesh.RESOLUTION + 1) sample.y = 1;

                            if (z == 0) sample.z = VoxelMesh.RESOLUTION;
                            else if (z == VoxelMesh.RESOLUTION + 1) sample.z = 1;

                            densityGrid[x, y, z] =  neigGrid.densityGrid[sample.x, sample.y, sample.z];
                            typeGrid[x, y, z] =     neigGrid.typeGrid[sample.x, sample.y, sample.z];
                        }
                    }
                }
            }
        }

        Vector3Int CoreGridFromVertex(int x, int y, int z) {
            Vector3Int offset = Vector3Int.zero;
            if (x == 0) offset.x = -1;
            else if (x == VoxelMesh.RESOLUTION + 1) offset.x = 1;

            if (y == 0) offset.y = -1;
            else if (y == VoxelMesh.RESOLUTION + 1) offset.y = 1;

            if (z == 0) offset.z = -1;
            else if (z == VoxelMesh.RESOLUTION + 1) offset.z = 1;

            return offset;
        }

        static bool GridExists(Vector3Int gridIndex) {
            return (gridIndex.x >= 0 && gridIndex.x < VoxelMesh.CONTAINERS_PER_SIDE && gridIndex.y >= 0 && gridIndex.y < VoxelMesh.CONTAINERS_PER_SIDE && gridIndex.z >= 0 && gridIndex.z < VoxelMesh.CONTAINERS_PER_SIDE);
        }

        public void GetFullGrids(out byte[] _fullDensityGrid, out byte[] _fullTypeGrid) {
            _fullDensityGrid =   Globals.CubeToLineArray(densityGrid);
            _fullTypeGrid =      Globals.CubeToLineArray(typeGrid);
        }

        public void ApplyDensityFunction(Brush.BrushStroke stroke, Vector3 gridOrigin) {
            // cache densityGrid for blur function
            if (stroke.brushMode == BrushMode.Smooth) {
                int side = densityGrid.GetLength(0);
                oldDensityGrid = new byte[side, side, side];
                oldTypeGrid = new byte[side, side, side];
                Array.Copy(densityGrid, oldDensityGrid, densityGrid.Length);
                Array.Copy(typeGrid, oldTypeGrid, typeGrid.Length);
            }
            for (int z = 1; z < VoxelMesh.RESOLUTION + 1; z++) {
                for (int y = 1; y < VoxelMesh.RESOLUTION + 1; y++) {
                    for (int x = 1; x < VoxelMesh.RESOLUTION + 1; x++) {
                        ApplyGridAction(x, y, z, gridOrigin, stroke);
                    }
                }
            }
            if (stroke.brushMode == BrushMode.Smooth) {
                Array.Clear(oldDensityGrid, 0, oldDensityGrid.Length);
                Array.Clear(oldTypeGrid, 0, oldTypeGrid.Length);
            }
        }

#region Voxel Grid actions
        public void ApplyGridAction(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke brushStroke) {
            switch (brushStroke.brushMode) {
                case BrushMode.Add:
                    DensityAction_Add(x, y, z, gridOrigin, brushStroke);
                    break;
                case BrushMode.Remove:
                    DensityAction_Remove(x, y, z, gridOrigin, brushStroke);
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
        public static float SampleDensity_Sphere(Vector3 sample, Vector3 origin, float radius) {
            return radius - (sample - origin).magnitude;
        }
        public static float SampleDensity_Plane(Vector3 sample, Vector3 origin, Vector3 normal) {
            float d = -(origin.x * normal.x + origin.y * normal.y + origin.z * normal.z);
            return -(sample.x * normal.x + sample.y * normal.y + sample.z * normal.z + d);
        }

        void DensityAction_Add(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            float functionDensity = SampleDensity_Sphere(new Vector3(x, y, z) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedFunctionDensity = Mathf.Clamp(functionDensity, -1, 1);
            byte encodedDensity = OctNodeData.EncodeDensity(clampedFunctionDensity);

            byte compareDist = densityGrid[x, y, z];
            // if this is a solid 'far' node, no need to add to it
            if (densityGrid[x, y, z] == 0 && typeGrid[x, y, z] != 0) compareDist = byte.MaxValue;

            if (encodedDensity > compareDist) {
                if (functionDensity != clampedFunctionDensity) {
                    densityGrid[x, y, z] = 0;
                } else {
                    densityGrid[x, y, z] = encodedDensity;
                }

                if (clampedFunctionDensity > 0) {
                    typeGrid[x, y, z] = Brush.selectedType;
                } else {
                    typeGrid[x, y, z] = 0;
                }
            }
        }
        void DensityAction_Remove(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // Cut into solid voxels, change type to 0 if voxel is no longer solid

            float functionDensity = -SampleDensity_Sphere(new Vector3(x, y, z) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedFunctionDensity = Mathf.Clamp(functionDensity, -1, 1);
            byte encodedDensity = OctNodeData.EncodeDensity(clampedFunctionDensity);

            byte compareDist = densityGrid[x, y, z];
            // if this is a solid 'far' node, totally need to add to it
            if (densityGrid[x, y, z] == 0 && typeGrid[x, y, z] != 0) compareDist = byte.MaxValue;

            if (encodedDensity < compareDist) {
                if (functionDensity != clampedFunctionDensity) {
                    densityGrid[x, y, z] = 0;
                } else {
                    densityGrid[x, y, z] = encodedDensity;
                }
                
                if (clampedFunctionDensity < 0) {
                    typeGrid[x, y, z] = 0;
                }
            }
        }
        void DensityAction_Paint(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // Paint voxels on the intersection of mesh and brush

            float functionDensity = SampleDensity_Sphere(new Vector3(x, y, z) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedFunctionDensity = Mathf.Clamp(functionDensity, -1, 1);

            if (functionDensity > 0 && OctNodeData.DecodeDensity(densityGrid[x, y, z]) > 0) {
                typeGrid[x, y, z] = Brush.selectedType;
            }
        }
        void DensityAction_Smooth(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {
            
            // basically Gaussian blur
            // If voxel is outside the brush, skip it

            if (SampleDensity_Sphere(new Vector3(x, y, z) + gridOrigin, stroke.brushLocation, stroke.brushRadius) < 0) {
                return;
            }
            
            int blurRadius = 2;
            bool solidBefore = oldDensityGrid[x, y, z] >= 126;
            int sum = 0;

            for (int k = z - blurRadius; k <= z + blurRadius; k++) {
                for (int j = y - blurRadius; j <= y + blurRadius; j++) {
                    for (int i = x - blurRadius; i <= x + blurRadius; i++) {
                        if (i != x || j != y || k != z) {
                            if (oldDensityGrid[i, j, k] == 0 && oldTypeGrid[i, j, k] != 0) {
                                sum += 252;
                            } else {
                                sum += oldDensityGrid[i, j, k];
                            }
                        }
                    }
                }
            }

            int count = (int)Mathf.Pow((1 + blurRadius * 2), 3) - 1;
            sum /= count;
            densityGrid[x, y, z] = (byte)(sum);

            // update type as well
            bool solidNow = densityGrid[x, y, z] >= 126;
            if (solidNow != solidBefore) {
                if (solidNow) {
                    typeGrid[x, y, z] = Brush.selectedType;
                } else {
                    typeGrid[x, y, z] = 0;
                }
            }
        }
        void DensityAction_Flatten(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {
            
            // Make voxels solid below surface & inside brush
            // Make voxels empty above surface & inside brush

            float planeDensity = SampleDensity_Plane(new Vector3(x, y, z) + gridOrigin, stroke.firstStrokePoint, stroke.firstStrokeNormal);
            float sphereDensity = SampleDensity_Sphere(new Vector3(x, y, z) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedDensity = Mathf.Clamp(planeDensity, -1, 1);
            byte encodedDensity = OctNodeData.EncodeDensity(clampedDensity);

            // only affect voxels inside spherical brush
            if (sphereDensity > 0) {
                if (planeDensity != clampedDensity) {
                    densityGrid[x, y, z] = 0; // far node
                } else {
                    densityGrid[x, y, z] = encodedDensity;
                }

                if (encodedDensity >= 126) {
                    typeGrid[x, y, z] = Brush.selectedType;
                } else {
                    typeGrid[x, y, z] = 0; // solid node
                }
            }
        }
    }
#endregion
}