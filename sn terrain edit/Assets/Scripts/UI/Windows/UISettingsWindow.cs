using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UISettingsWindow : UIWindow
{
    public void ApplySettings() {

        bool displayTypeColors = transform.GetChild(2).GetChild(4).GetChild(1).GetComponent<UICheckbox>().check;

        Globals.get.UpdateBatchMaterial(displayTypeColors);
    }
}
