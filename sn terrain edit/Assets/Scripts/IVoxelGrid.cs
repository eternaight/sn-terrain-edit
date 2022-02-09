namespace ReefEditor {
    public interface IVoxelGrid {
        byte[] GetVoxel(int x, int y, int z);
        bool GetMask(int x, int y, int z);
    }
}
