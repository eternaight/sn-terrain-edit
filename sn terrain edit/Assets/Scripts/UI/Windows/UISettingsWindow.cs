using UnityEngine.UI;
using SFB;
using UnityEngine;

namespace ReefEditor.UI {
    public class UISettingsWindow : UIWindow {
        UICheckbox fullscreenCheckbox;

        private void Start() {
            fullscreenCheckbox = GetComponentInChildren<UICheckbox>();
            fullscreenCheckbox.SetState(false);
            fullscreenCheckbox.transform.GetComponent<Button>().onClick.AddListener(UpdateFullscreenMode);
        }

        public void BrowseGamePath() {
            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select a Subnautica or Below Zero game folder.", Application.dataPath, false);
            if (paths.Length != 0) {
                EditorManager.SetGamePath(paths[0], true);
                UpdatePathDisplay(paths[0]);
            }
        }
        public override void EnableWindow()
        {
            UpdatePathDisplay(EditorManager.GetGamePath());
            base.EnableWindow();
        }
        private void UpdatePathDisplay(string path) => transform.GetChild(1).GetChild(1).GetComponent<Text>().text = path;

        private void UpdateFullscreenMode() { Screen.fullScreenMode = (fullscreenCheckbox.check ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed); }
    }
}