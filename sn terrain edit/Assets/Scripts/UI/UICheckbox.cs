using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UICheckbox : MonoBehaviour {
        public bool check;
        Image checkImage;
        void Awake() {
            checkImage = transform.GetChild(0).GetComponent<Image>();
        }
        public void OnPressed() {
            check = !check; 
            checkImage.enabled = check;
        }
        public void SetState(bool _check) {
            check = _check;
            checkImage.enabled = _check;
        }
    }
}