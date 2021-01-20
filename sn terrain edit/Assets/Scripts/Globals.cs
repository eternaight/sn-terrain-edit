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
    public const int threadGroupSize = 1;

    public const string sourcePathKey = "sourcePath";
    public const string outputPathKey = "outputPath";
    public const string gameToBatchPostfix = "\\Subnautica_Data\\StreamingAssets\\SNUnmanagedData\\Build18\\CompiledOctreesCache";

    void Awake() {
        get = this;
        GenerateColorMap();
    }

    void OnValidate() {
        get = this;
        GenerateColorMap();
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

    void GenerateColorMap() {
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
            batchMat.color = Color.grey;
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

}