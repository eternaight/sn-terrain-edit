using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonSelect : MonoBehaviour
{
    public int selection;
    public int totalButtons;
    public Color unselectedColor;
    public Color selectedColor;

    public event Action OnValueChanged;

    void Start() {
        UpdateButtons();
    }
    public void SetSelection(int newSel) {
        selection = newSel;
        UpdateButtons();
        OnValueChanged();
    }
    void UpdateButtons() {
        for (int i = 0; i < totalButtons; i++) {
            Image buttonImage = transform.GetChild(i).GetComponent<Image>();
            buttonImage.color = (i == selection) ? selectedColor : unselectedColor;
        }
    }
}
