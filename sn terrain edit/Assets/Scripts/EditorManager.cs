using ReefEditor.ContentLoading;
using System;
using System.IO;
using UnityEngine;

namespace ReefEditor {
    public class EditorManager : MonoBehaviour {

        public static EditorManager instance;
        private SNContentLoader materialLoader;
        private LoadingManager loadingManager;

        [SerializeField] private Material batchMat;
        [SerializeField] private Material batchCappedMat;
        [SerializeField] private Material boundaryGizmoMat;
        [SerializeField] private Color[] brushColors;
        
        private bool belowzero;
        public static bool BelowZero { get { return instance.belowzero; } }
        private string gamePath;
        private string userBatchOutputPath;

        private GameObject[] boundaryPlanes;

        public const int threadGroupSize = 8;
        public const string sourcePathKey = "gamePath";
        public const string outputPathKey = "outputPath";
        public const string dataToUnmanaged = "StreamingAssets\\SNUnmanagedData";
        public const string dataToAddressables = "StreamingAssets\\aa\\StandaloneWindows64";

        public string batchSourcePath {
            get {
                return Path.Combine(gamePath, gameDataFolder, dataToUnmanaged, gameExportWindow, "CompiledOctreesCache");
            }
        }
        public string batchOutputPath {
            get {
                return userBatchOutputPath;
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

        public event Action OnContentLoaded;

        private void Awake() {
            instance = this;
            materialLoader = new SNContentLoader();
            loadingManager = new LoadingManager();
        }
        private void Start() {
            VoxelMetaspace.instance.OnRegionLoaded += RedrawBoundaryPlanes;
        }
        private void Update() {
            loadingManager.UpdateLoading();
        }

        public static Color ColorFromType(int type) {
            UnityEngine.Random.InitState(type);
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        public static Material GetDefaultTriplanarMaterial() {
            return instance.batchMat;
        }
        public static Material GetCappedTriplanarMaterial() {
            return instance.batchCappedMat;
        }

        public static string GetGamePath() => instance.gamePath;
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

        public static void RedrawBoundaryPlanes() {
            GameObject[] planes;
            if (instance.boundaryPlanes == null) {
                planes = new GameObject[6];
                instance.boundaryPlanes = planes;
                for(int c = 0; c < 6; c++) {
                    planes[c] = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    planes[c].transform.SetParent(instance.transform);
                    planes[c].GetComponent<MeshRenderer>().material = instance.boundaryGizmoMat;
                }
                // bottom
                planes[0].transform.eulerAngles = Vector3.zero;
                // top
                planes[1].transform.eulerAngles = Vector3.right * 180;
                // left
                planes[2].transform.eulerAngles = Vector3.forward * -90;
                // right
                planes[3].transform.eulerAngles = Vector3.forward * 90;
                // back
                planes[4].transform.eulerAngles = Vector3.right * 90;
                // forward
                planes[5].transform.eulerAngles = Vector3.right * -90;
            } else {
                planes = instance.boundaryPlanes;
            }

            Vector3 worldCenter = (Vector3)VoxelMetaspace.instance.RealSize * 0.5f;

            planes[0].transform.position = new Vector3(worldCenter.x, 0, worldCenter.z);
            planes[0].transform.localScale = new Vector3(worldCenter.x * .2f, 1, worldCenter.z * .2f);

            planes[1].transform.position = new Vector3(worldCenter.x, worldCenter.y * 2, worldCenter.z);
            planes[1].transform.localScale = new Vector3(worldCenter.x * .2f, 1, worldCenter.z * .2f);

            planes[2].transform.position = new Vector3(0, worldCenter.y, worldCenter.z);
            planes[2].transform.localScale = new Vector3(worldCenter.y * .2f, 1, worldCenter.z * .2f);

            planes[3].transform.position = new Vector3(worldCenter.x * 2, worldCenter.y, worldCenter.z);
            planes[3].transform.localScale = new Vector3(worldCenter.y * .2f, 1, worldCenter.z * .2f);

            planes[4].transform.position = new Vector3(worldCenter.x, worldCenter.y, 0);
            planes[4].transform.localScale = new Vector3(worldCenter.x * .2f, 1, worldCenter.y * .2f);

            planes[5].transform.position = new Vector3(worldCenter.x, worldCenter.y, worldCenter.z * 2);
            planes[5].transform.localScale = new Vector3(worldCenter.x * .2f, 1, worldCenter.y * .2f);
        }

        public static void UpdateBoundaryMaterial(Vector3 newPos, float radius) {
            instance.boundaryGizmoMat.SetVector("_CursorWorldPos", newPos);
            instance.boundaryGizmoMat.SetFloat("_BlendRadius", radius);
        }

        public static bool CheckIsGamePathValid() {
            return Directory.Exists(instance.batchSourcePath) && Directory.Exists(instance.resourcesSourcePath);
        }

        public static SNContentLoader GetContentLoader() => instance.materialLoader;
        public static LoadingManager GetLoading() => instance.loadingManager;
        public static Color[] GetBrushModesPalette() => instance.brushColors;

        public static void InitiateMaterialsLoad() {
            if (instance.OnContentLoaded != null)
                instance.loadingManager.OnQueueEmpty += instance.OnContentLoaded;
            instance.loadingManager.AddLoader(instance.materialLoader);
        }
    }
}