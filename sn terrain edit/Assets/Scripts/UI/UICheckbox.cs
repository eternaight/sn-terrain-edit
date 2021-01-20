using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICheckbox : MonoBehaviour
{
    public bool check;
    Image checkImage;
    void Awake() {
        checkImage = transform.GetChild(0).GetComponent<Image>();
    }
    public void OnPressed() {
        check = !check; 
        checkImage.enabled = check;
    }
}
