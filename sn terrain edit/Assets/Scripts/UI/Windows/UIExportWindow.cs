using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIExportWindow : UIWindow {
        UICheckbox checkbox;
        UIButtonSelect modeSelection;

        GameObject checkboxGroup => transform.GetChild(2).GetChild(1).gameObject;

        public void Export() {
            if (!VoxelWorld.aRegionIsLoaded) {
                EditorUI.DisplayErrorMessage("Nothing to export!");
                return;
            }

            if (modeSelection.selection == 1) {
                // Export patch
                string path = StandaloneFileBrowser.SaveFilePanel("Save patch as...", Application.dataPath, "TerrainPatch", "optoctreepatch");
                if (string.IsNullOrEmpty(path)) {
                    // user cancels
                    return;
                }
                Globals.instance.userBatchOutputPath = path;
            }
            else {
                // Export some optoctrees files
                string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select export folder...", Application.dataPath, false);
                if (paths.Length == 0) {
                    // user cancels
                    return;
                }
                Globals.instance.userBatchOutputPath = paths[0];
            }

            VoxelWorld.OnRegionExported += EditorUI.DisableStatusBar;
            EditorUI.UpdateStatusBar("Exporting...", 1);

            VoxelWorld.ExportRegion(modeSelection.selection);
        }

        public void OnCheckboxInteract() {
            Globals.instance.exportIntoGame = checkbox.check;
        }

        public void OnModeChanged() {
            if (modeSelection.selection != 0) {
                checkboxGroup.SetActive(false);
                Globals.instance.exportIntoGame = false;
            } else {
                checkboxGroup.SetActive(true);
                OnCheckboxInteract();
            }
        }

        // overrides
        public override void EnableWindow()
        {
            if (checkbox is null) {
                checkbox = GetComponentInChildren<UICheckbox>();
                checkbox.transform.GetComponent<Button>().onClick.AddListener(OnCheckboxInteract);
                OnCheckboxInteract();
            }
            if (modeSelection is null) {
                modeSelection = GetComponentInChildren<UIButtonSelect>();
                modeSelection.OnSelectionChanged += OnModeChanged;
                OnModeChanged();
            }

            SetFileCountStrings();

            base.EnableWindow();
        }
        private void SetFileCountStrings() {
            if (!VoxelWorld.aRegionIsLoaded) {
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = "No batches loaded";
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
                return;
            }

            int batchCount = VoxelWorld.regionSize.x * VoxelWorld.regionSize.y * VoxelWorld.regionSize.z;
            if (batchCount == 1)
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = "1 file";
            else
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = $"{batchCount} files";
            transform.GetChild(2).GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
        }
    }
}