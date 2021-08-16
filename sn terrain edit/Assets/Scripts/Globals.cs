using System.IO;
using System.Linq;
using UnityEngine;

namespace ReefEditor {
    public class Globals : MonoBehaviour {

        public static Globals instance;

        public Material batchMat;
        public Material batchCappedMat;
        public Material brushGizmoMat;
        public Material simpleMapMat;
        public Color[] brushColors;
        public string gamePath;
        public bool belowzero;
        public string userBatchOutputPath;

        public string batchSourcePath {
            get {
                return Path.Combine(gamePath, gameDataFolder, dataToUnmanaged, gameExportWindow, "CompiledOctreesCache");
            }
        }
        public string batchOutputPath {
            get {
                return exportIntoGame ? batchSourcePath : userBatchOutputPath;
            }
        }
        public string gameDataFolder {
            get {
                return belowzero ? "SubnauticaZero_Data" : "Subnautica_Data";
            }
        }
        public string gameExportWindow {
            get {
                return belowzero ? "Expansion" : "Build18";
            }
        }
        public string resourcesSourcePath {
            get {
                return Path.Combine(gamePath, gameDataFolder);
            }
        }
        public string blocktypeStringsFilename {
            get {
                return belowzero ? "blocktypeStringsBZ" : "blocktypeStrings";
            }
        }
        
        public const int threadGroupSize = 8;

        public const string sourcePathKey = "gamePath";
        public const string outputPathKey = "outputPath";
        public const string dataToUnmanaged = "StreamingAssets\\SNUnmanagedData";
        public const string dataToAddressables = "StreamingAssets\\aa\\StandaloneWindows64";
        public bool exportIntoGame;

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
            string filename = Path.Combine(instance.gamePath, instance.gameDataFolder, dataToUnmanaged, instance.gameExportWindow, "biomemap.png");

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
            string[] splitdirs = path.Split('\\');
            instance.belowzero = splitdirs[splitdirs.Length - 1] == "SubnauticaZero";
            
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
    }
}