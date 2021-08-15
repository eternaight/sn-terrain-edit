using UnityEngine;

namespace ReefEditor.UI {
    public class UIWindow : MonoBehaviour {
        bool windowActive = false;
        public virtual void DisableWindow() {
            windowActive = false;
            gameObject.SetActive(windowActive);
        }
        public virtual void EnableWindow() {
            windowActive = true;
            PushToTop();
            gameObject.SetActive(windowActive);
        }
        public void ToggleWindow() {
            if (windowActive) DisableWindow();
            else EnableWindow();
        }

        public void PushToTop() {
            transform.SetAsLastSibling();
        }
    }
}