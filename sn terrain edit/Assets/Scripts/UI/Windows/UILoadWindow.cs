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

        // InputField rangeStartInput = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<InputField>();
        // InputField rangeEndInput = transform.GetChild(2).GetChild(2).GetChild(2).GetComponent<InputField>();

        // string[] s = rangeStartInput.text.Split(' ');
        // Vector3Int start = new Vector3Int(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
        // s = rangeEndInput.text.Split(' ');
        // Vector3Int end = new Vector3Int(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));

        RegionLoader.loader.LoadSimpleMap();
        RegionLoader.loader.OnLoadFinish += EditorUI.DisableStatusBar;
    }
}
