using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIProgressBar : MonoBehaviour
{
    Image barImage;
    public void SetFill(float amount) {
        if (barImage == null) {
            barImage = transform.GetChild(1).GetComponent<Image>();
        }
        barImage.fillAmount = amount;
    }
}
