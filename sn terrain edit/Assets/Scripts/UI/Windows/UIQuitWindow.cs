using UnityEngine;

namespace ReefEditor.UI {
    public class UIQuitWindow : UIWindow {
        public void CloseApp() {
            Application.Quit();
        }
    }
}
