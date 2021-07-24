using System;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIButtonSelect : MonoBehaviour {
        public int selection;
        int totalButtons;
        public Color unselectedColor;
        public Color selectedColor;

        public event Action OnSelectionChanged;

        void Start() {
            totalButtons = transform.childCount;
            UpdateButtons();
        }
        public void SetSelection(int newSel) {
            selection = newSel;
            UpdateButtons();
            OnSelectionChanged();
        }
        void UpdateButtons() {
            for (int i = 0; i < totalButtons; i++) {
                Image buttonImage = transform.GetChild(i).GetComponent<Image>();
                buttonImage.color = (i == selection) ? selectedColor : unselectedColor;
            }
        }
    }
}