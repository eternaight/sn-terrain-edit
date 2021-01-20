using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIExportWindow : UIWindow
{
    public void ExportBatch() {

        if (string.IsNullOrEmpty(Globals.get.batchOutputPath)) {
            // throw error
            return;
        }

        EditorUI.UpdateStatusBar("Exporting batch... ", 1);

        RegionLoader.loader.OnRegionSaved += EditorUI.DisableStatusBar;
        RegionLoader.loader.SaveRegion();
    }

    public void SetExportPath() {
        
        TMP_InputField fieldInput = transform.GetChild(2).GetChild(1).GetComponent<TMP_InputField>();
        fieldInput.text = Globals.get.batchOutputPath;
    }
    public void SaveNewExportPath() {
        TMP_InputField fieldI = transform.GetChild(2).GetChild(1).GetComponent<TMP_InputField>();

        if (fieldI.text != "") {
            Globals.SetBatchOutputPath(fieldI.text, true);
        }
    }

    // overrides
    public override void EnableWindow()
    {
        SetExportPath();
        base.EnableWindow();
    }
    public override void DisableWindow()
    {
        SaveNewExportPath();
        base.DisableWindow();
    }
}
