using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILoadWindow : UIWindow
{
    public void LoadBatch() {

        if (string.IsNullOrEmpty(Globals.get.batchSourcePath)) {
            // throw error
            return;
        }

        InputField field1 = transform.GetChild(2).GetChild(3).GetComponent<InputField>();

        string[] s = field1.text.Split(' ');

        Vector3Int start = new Vector3Int(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
        Vector3Int end = start;

        RegionLoader.loader.LoadRegion(start, end);
        RegionLoader.loader.OnLoadFinish += EditorUI.DisableStatusBar;
    }

    public void SetLoadPath() {
        
        InputField fieldInput2 = transform.GetChild(2).GetChild(1).GetComponent<InputField>();
        fieldInput2.text = Globals.get.batchSourcePath;
    }
    public void SaveNewLoadPath() {
        InputField fieldI = transform.GetChild(2).GetChild(1).GetComponent<InputField>();

        if (fieldI.text != "") {
            Globals.SetBatchInputPath(fieldI.text, true);
        }
    }

    // overrides
    public override void EnableWindow()
    {
        SetLoadPath();
        base.EnableWindow();
    }
    public override void DisableWindow()
    {
        SaveNewLoadPath();
        base.DisableWindow();
    }
}
