using UnityEngine.UI;
using SFB;
using UnityEngine;

namespace ReefEditor.UI {
    public class UISettingsWindow : UIWindow
    {
        public void BrowseGamePath() {
            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select a Subnautica or Below Zero game folder.", Application.dataPath, false);
            if (paths.Length != 0) {
                Globals.SetGamePath(paths[0], true);
                UpdatePathDisplay(paths[0]);
            }
        }
        public override void EnableWindow()
        {
            UpdatePathDisplay(Globals.instance.gamePath);
            base.EnableWindow();
        }
        private void UpdatePathDisplay(string path) => transform.GetChild(1).GetChild(1).GetComponent<Text>().text = path;
    }
}