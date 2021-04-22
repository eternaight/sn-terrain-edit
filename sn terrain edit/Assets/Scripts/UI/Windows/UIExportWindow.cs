using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIExportWindow : UIWindow
{
    UICheckbox checkbox;
    public void ExportBatch() {

        if (string.IsNullOrEmpty(Globals.instance.batchOutputPath)) {
            // throw error
            return;
        }

        EditorUI.UpdateStatusBar("Exporting batch... ", 1);

        RegionLoader.loader.OnRegionSaved += EditorUI.DisableStatusBar;
        RegionLoader.loader.SaveRegion();
    }

    public void SetExportPath() {
        
        InputField fieldInput = transform.GetChild(2).GetChild(2).GetComponent<InputField>();
        fieldInput.text = Globals.instance.userBatchOutputPath;
    }
    public void SaveNewExportPath() {
        InputField fieldI = transform.GetChild(2).GetChild(2).GetComponent<InputField>();

        if (fieldI.text != "") {
            Globals.SetBatchOutputPath(fieldI.text, true);
        }
    }

    public void OnCheckboxInteract() {
        if (checkbox.check) {
            // export into the game folders
            Globals.instance.exportIntoGame = true;
            transform.GetChild(2).GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).GetChild(2).gameObject.SetActive(false);
        } else {
            // export into custom folder
            Globals.instance.exportIntoGame = false;
            transform.GetChild(2).GetChild(1).gameObject.SetActive(true);
            transform.GetChild(2).GetChild(2).gameObject.SetActive(true);
        }
    }

    // overrides
    public override void EnableWindow()
    {
        SetExportPath();
        if (checkbox == null) {
            checkbox = GetComponentInChildren<UICheckbox>();
            checkbox.transform.GetComponent<Button>().onClick.AddListener(OnCheckboxInteract);
            OnCheckboxInteract();
        }
        base.EnableWindow();
    }
    public override void DisableWindow()
    {
        SaveNewExportPath();
        base.DisableWindow();
    }
}
