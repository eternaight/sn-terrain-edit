using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIExportWindow : UIWindow {
        UICheckbox checkbox;
        UIButtonSelect modeSelection;

        GameObject checkboxGroup => transform.GetChild(2).GetChild(1).gameObject;
        GameObject folderInputGroup => transform.GetChild(2).GetChild(2).gameObject;

        public void Export() {

            if (string.IsNullOrEmpty(Globals.instance.batchOutputPath)) {
                EditorUI.DisplayErrorMessage("No output path set.");
                return;
            }

            VoxelWorld.OnRegionLoaded += EditorUI.DisableStatusBar;
            EditorUI.UpdateStatusBar("Exporting...", 1);
            VoxelWorld.ExportRegion(modeSelection.selection == 1);
        }

        public void SetExportPath() {
            
            InputField fieldInput = folderInputGroup.GetComponentInChildren<InputField>();
            fieldInput.text = Globals.instance.userBatchOutputPath;
        }
        public void SaveNewExportPath() {
            InputField fieldI = folderInputGroup.GetComponentInChildren<InputField>();

            if (fieldI.text != "") {
                Globals.SetBatchOutputPath(fieldI.text, true);
            }
        }

        public void OnCheckboxInteract() {
            Globals.instance.exportIntoGame = checkbox.check;
            folderInputGroup.SetActive(!checkbox.check);
        }

        public void OnModeChanged() {
            if (modeSelection.selection == 1) {
                checkboxGroup.SetActive(false);
                folderInputGroup.SetActive(true);
                Globals.instance.exportIntoGame = false;
            } else {
                checkboxGroup.SetActive(true);
                OnCheckboxInteract();
            }
        }

        // overrides
        public override void EnableWindow()
        {
            SetExportPath();
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
        public override void DisableWindow()
        {
            SaveNewExportPath();
            base.DisableWindow();
        }
    }
}