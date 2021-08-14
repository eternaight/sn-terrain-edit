using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIHybridInput : MonoBehaviour, IDragHandler, IPointerClickHandler {

        float value;
        float realWidth;
        UIProgressBar bar;
        InputField field;
        RectTransform rectTf;

        private void Start() {
            bar = GetComponentInChildren<UIProgressBar>();
            
            field = GetComponentInChildren<InputField>();
            field.enabled = false;
            field.onEndEdit.AddListener(DisableInputField);

            rectTf = transform as RectTransform;
            
            realWidth = GetComponentInParent<Canvas>().scaleFactor * rectTf.rect.width;
            Redraw();
        }
 
        public void OnDrag(PointerEventData eventData) {
            float barStart = transform.position.x - realWidth / 2;
            value = Mathf.Clamp01((eventData.position.x - barStart) / realWidth);
            Redraw();
        }

        public void OnPointerClick(PointerEventData eventData) {
            field.enabled = true;
            field.Select();
        }

        private void DisableInputField(string val) {
            float.TryParse(val, out value);
            field.enabled = false;
            Redraw();
        }

        private void Redraw() {
            bar.SetFill(value);
            field.SetTextWithoutNotify(Math.Round(value, 3).ToString());
        }
    }
}
