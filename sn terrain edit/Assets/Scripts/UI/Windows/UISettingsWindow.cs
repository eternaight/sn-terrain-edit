using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UISettingsWindow : UIWindow
    {
        public void ApplySettings() {
            Globals.SetGamePath(transform.GetComponentInChildren<InputField>().text, true);
        }
        public override void EnableWindow()
        {
            transform.GetComponentInChildren<InputField>().text = Globals.instance.gamePath;
            base.EnableWindow();
        }
    }
}