using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsWindow : UIWindow
{
    public void ApplySettings() {
        Globals.SetGamePath(transform.GetChild(2).GetChild(1).GetComponent<InputField>().text, true);
    }
    public void CloseApp() {
        Application.Quit();
    }
    public override void EnableWindow()
    {
        transform.GetChild(2).GetChild(1).GetComponent<InputField>().text = Globals.instance.gamePath;
        base.EnableWindow();
    }
}
