using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst;
        [SerializeField] RectTransform statusBar;

        void Start() {
            inst = this;
        }

        public static void UpdateStatusBar(string title, float val) {
            inst.statusBar.gameObject.SetActive(true);

            inst.statusBar.GetChild(0).GetComponent<Text>().text = title;
            inst.statusBar.GetChild(1).GetComponent<UIProgressBar>().SetFill(val);
        }
        public static void DisableStatusBar() {
            inst.statusBar.gameObject.SetActive(false);
        }
    }
}