using System;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonSelect : MonoBehaviour {
    public int selection;
    int totalButtons;
    public Color unselectedColor;
    public Color selectedColor;

    public event Action OnSelectionChanged;

    void Start() {
        totalButtons = transform.childCount;
        for (int childIndex = 0; childIndex < transform.childCount; childIndex++) {
            Button butt = transform.GetChild(childIndex).GetComponent<Button>();
            if (butt) {
                butt.onClick.AddListener(() => SetSelection(butt));
            }
        }

        try { UpdateButtons(); }
        catch (NullReferenceException) {}
    }
    void SetSelection(Button buttonClicked) {
        selection = buttonClicked.transform.GetSiblingIndex();
        UpdateButtons();
        OnSelectionChanged?.Invoke();
    }
    public void SetSelectionFromBrushUpdate(int _selection) {
        selection = _selection;
        UpdateButtons();
    }
    void UpdateButtons() {
        for (int i = 0; i < totalButtons; i++) {
            Image buttonImage = transform.GetChild(i).GetComponent<Image>();
            buttonImage.color = (i == selection) ? selectedColor : unselectedColor;
        }
    }
}