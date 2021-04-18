using UnityEngine;
public class Globals : MonoBehaviour {

    public static Globals get;

    [HideInInspector]
    public bool displayTypeColors;
    public Material batchMat;
    public Material brushGizmoMat;
    public Texture2D colorMap;
    public Color[] brushColors;
    public string batchSourcePath;
    public string batchOutputPath;
    public const int threadGroupSize = 8;

    public const string sourcePathKey = "sourcePath";
    public const string outputPathKey = "outputPath";
    public const string gameToBatchPostfix = "\\Subnautica_Data\\StreamingAssets\\SNUnmanagedData\\Build18\\CompiledOctreesCache";
    public const string gameToAddressablesPostfix = "\\Subnautica_Data\\StreamingAssets\\aa\\StandaloneWindows64";

    int type = 0;

    void Awake() {
        get = this;
    }

    public static Color ColorFromType(int type) {

        Random.InitState(type);
        return new Color(Random.value, Random.value, Random.value);

    }

    public static Material GetBatchMat() {
        return get.batchMat;
    }
    public static Texture2D GetColorMap() {
        return get.colorMap;
    }

    public void GenerateColorMap() {
        colorMap = new Texture2D(16, 16);

        for(int y = 0; y < 16; y++) {
            for(int x = 0; x < 16; x++) {
                colorMap.SetPixel(x, y, ColorFromType(x + y * 16));
            }
        }

        colorMap.filterMode = FilterMode.Point;
        colorMap.Apply();
        UpdateBatchMaterial(displayTypeColors);
    }

    public void UpdateBatchMaterial(bool display) {

        displayTypeColors = display;

        if (displayTypeColors) {
            batchMat.color = Color.white;
            batchMat.SetTexture("_MainTex", colorMap);
        } else {
            batchMat.color = Color.white;
            batchMat.SetTexture("_MainTex", null);
        }
    }

    public static void SetBatchInputPath(string path, bool save) {
        get.batchSourcePath = path;
        
        if (save)
            SaveData.WriteKey(sourcePathKey, path);
    }
    public static void SetBatchOutputPath(string path, bool save) {
        get.batchOutputPath = path;

        if (save)
            SaveData.WriteKey(outputPathKey, path);
    }

    public static int LinearIndex(int x, int y, int z, int dim) {
        return x + y * dim + z * dim * dim;
    }
    public static int LinearIndex(int x, int y, int z, Vector3Int dim) {
        return x + y * dim.x + z * dim.x * dim.y;
    }
    
    public static byte[] _3DArrayTo1D(byte[,,] array) {
        Vector3Int size = new Vector3Int(array.GetLength(0), array.GetLength(1), array.GetLength(2));
        byte[] new_array = new byte[size.x * size.y * size.z];

        for (int k = 0; k < size.z; ++k) {
            for (int j = 0; j < size.y; ++j) {
                for (int i = 0; i < size.x; ++i) {
                    new_array[LinearIndex(i, j, k, size)] = array[i, j, k];
                }
            }
        }

        return new_array;
    }
    public static byte[,,] _1DArrayTo3D(byte[] array, Vector3Int size) {
        byte[,,] new_array = new byte[size.x, size.y, size.z];

        for (int k = 0; k < size.z; ++k) {
            for (int j = 0; j < size.y; ++j) {
                for (int i = 0; i < size.x; ++i) {
                    new_array[i, j, k] = array[LinearIndex(i, j, k, size)];
                }
            }
        }

        return new_array;
    }

    public static void SetMatBlockType(byte blockType) {
        Globals.get.type = blockType;
    }
}