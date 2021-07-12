using System.IO;
using UnityEngine;
public class Globals : MonoBehaviour {

    public static Globals instance;

    [HideInInspector]
    public bool displayTypeColors;
    public Material batchMat;
    public Material batchCappedMat;
    public Material brushGizmoMat;
    public Material simpleMapMat;
    public Texture2D colorMap;
    public Color[] brushColors;
    public string gamePath;
    public string batchSourcePath {
        get {
            return gamePath + gameToBatchPostfix;
        }
    }
    public string batchOutputPath {
        get {
            return exportIntoGame ? gamePath + gameToBatchPostfix : userBatchOutputPath;
        }
    }
    public string userBatchOutputPath;
    public const int threadGroupSize = 8;

    public const string sourcePathKey = "gamePath";
    public const string outputPathKey = "outputPath";
    public const string gameToBatchPostfix = "\\Subnautica_Data\\StreamingAssets\\SNUnmanagedData\\Build18\\CompiledOctreesCache";
    public const string gameToAddressablesPostfix = "\\Subnautica_Data\\StreamingAssets\\aa\\StandaloneWindows64";
    public bool exportIntoGame;

    int type = 0;

    void Awake() {
        instance = this;
    }

    public static Color ColorFromType(int type) {

        Random.InitState(type);
        return new Color(Random.value, Random.value, Random.value);

    }

    public static Material GetBatchMat() {
        return instance.batchMat;
    }
    public static void BakeSimpleMapMaterial() {
        string filename = instance.gamePath + "\\Subnautica_Data\\StreamingAssets\\SNUnmanagedData\\Build18\\biomemap.png";

        if (File.Exists(filename)) {
            byte[] fileData = File.ReadAllBytes(filename);
            Texture2D mapTexture = new Texture2D(2, 2);
            if (ImageConversion.LoadImage(mapTexture, fileData, false)) {
                instance.simpleMapMat.mainTexture = mapTexture;
            }
        }
    }
    public static Material GetSimpleMapMat() {
        return instance.simpleMapMat;
    }

    public static void SetGamePath(string path, bool save) {
        instance.gamePath = path;
        
        if (save)
            SaveData.WriteKey(sourcePathKey, path);
    }
    public static void SetBatchOutputPath(string path, bool save) {
        instance.userBatchOutputPath = path;

        if (save)
            SaveData.WriteKey(outputPathKey, path);
    }

    public static int LinearIndex(int x, int y, int z, int dim) {
        return x + y * dim + z * dim * dim;
    }
    public static int LinearIndex(int x, int y, int z, Vector3Int dim) {
        return x + y * dim.x + z * dim.x * dim.y;
    }
    
    public static byte[] CubeToLineArray(byte[,,] array) {
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
    public static byte[,,] LineToCubeArray(byte[] array, Vector3Int size) {
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
        Globals.instance.type = blockType;
    }
}