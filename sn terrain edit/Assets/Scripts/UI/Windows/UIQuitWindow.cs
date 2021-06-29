using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQuitWindow : UIWindow
{
    public void CloseApp() {
        Application.Quit();
    }
}
