using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILoadWindow : UIWindow
{
    public void LoadBatch() {

        if (string.IsNullOrEmpty(Globals.instance.gamePath)) {
            // display error
            return;
        }

        InputField batchIndexInput = transform.GetChild(2).GetChild(1).GetComponent<InputField>();
        string[] s = batchIndexInput.text.Split(' ');

        Vector3Int start = new Vector3Int(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
        Vector3Int end = start;

        RegionLoader.loader.LoadRegion(start, end);
        RegionLoader.loader.OnLoadFinish += EditorUI.DisableStatusBar;
    }
}
