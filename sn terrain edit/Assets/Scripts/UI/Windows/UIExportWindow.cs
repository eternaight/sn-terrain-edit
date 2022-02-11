using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIExportWindow : UIWindow {
        UICheckbox checkbox;
        UIButtonSelect modeSelection;
        private bool exportIntoCache;

        GameObject checkboxGroup => transform.GetChild(2).GetChild(1).gameObject;

        public void Export() {
            if (!VoxelMetaspace.instance.regionLoaded) {
                EditorUI.DisplayErrorMessage("Nothing to export!");
                return;
            }

            switch (modeSelection.selection) {
                case 0:
                    // Export some .optoctrees files
                    if (!exportIntoCache) {
                        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select export folder...", Application.dataPath, false);
                        if (paths.Length == 0) {
                            // user cancels
                            return;
                        }
                        EditorManager.SetBatchOutputPath(paths[0], true);
                    } else {
                        EditorManager.SetBatchOutputPath(EditorManager.instance.batchSourcePath, false);
                    }
                    break;
                case 1:
                    // Save .optoctreepatch file
                    string path = StandaloneFileBrowser.SaveFilePanel("Save patch as...", Application.dataPath, "TerrainPatch", "optoctreepatch");
                    if (string.IsNullOrEmpty(path)) {
                        // user cancels
                        return;
                    }
                    EditorManager.SetBatchOutputPath(path, true);
                    break;
                case 2:
                    // Save .fbx file
                    path = StandaloneFileBrowser.SaveFilePanel("Save mesh as...", Application.dataPath, "SubnauticaScene", "fbx");
                    if (string.IsNullOrEmpty(path)) {
                        // user cancels
                        return;
                    }
                    EditorManager.SetBatchOutputPath(path, true);
                    break;
            }

            VoxelMetaspace.instance.OnRegionExported += EditorUI.DisableStatusBar;
            EditorUI.UpdateStatusBar("Exporting...", 1);

            VoxelMetaspace.InitiateRegionExport(modeSelection.selection);
        }

        public void OnCheckboxInteract() {
            exportIntoCache = checkbox.check;
        }

        public void OnModeChanged() {
            if (modeSelection.selection != 0) {
                checkboxGroup.SetActive(false);
                exportIntoCache = false;
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
            if (!VoxelMetaspace.instance.regionLoaded) {
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = "No batches loaded";
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
                return;
            }

            int batchCount = VoxelMetaspace.instance.CountBatches();
            if (batchCount == 1)
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = "1 file";
            else
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = $"{batchCount} files";
            transform.GetChild(2).GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
        }
    }
}