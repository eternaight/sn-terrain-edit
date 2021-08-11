using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst;
        [SerializeField] RectTransform statusBar;
        public GameObject errorPrefab;

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

        public static void DisplayErrorMessage(string message) {
            // clear previous error if it exists
            if (inst.statusBar.parent.childCount > 1) {
                Destroy(inst.statusBar.parent.GetChild(1).gameObject);
            }
            GameObject go = Instantiate(inst.errorPrefab, inst.statusBar.parent);
            go.transform.SetAsLastSibling();
        }
    }
}