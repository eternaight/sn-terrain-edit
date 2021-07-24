using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIProgressBar : MonoBehaviour {
        Image barImage;
        public void SetFill(float amount) {
            if (barImage == null) {
                barImage = transform.GetChild(1).GetComponent<Image>();
            }
            barImage.fillAmount = amount;
        }
    }
}