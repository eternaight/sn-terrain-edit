using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILoadWindow : UIWindow
{
    const bool MAPLOAD = true;
    public void LoadBatch() {

        if (string.IsNullOrEmpty(Globals.instance.gamePath)) {
            // display error
            return;
        }

        if (MAPLOAD) {
            RegionLoader.loader.LoadMap();
            RegionLoader.loader.OnLoadFinish += EditorUI.DisableStatusBar;
        } else {
            InputField batchIndexInput = transform.GetChild(2).GetChild(1).GetComponent<InputField>();
            string[] s = batchIndexInput.text.Split(' ');

            Vector3Int index = new Vector3Int(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
            RegionLoader.loader.LoadSingleBatch(index);
            RegionLoader.loader.OnLoadFinish += EditorUI.DisableStatusBar;
        }
    }
}
