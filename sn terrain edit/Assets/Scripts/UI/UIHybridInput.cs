using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIHybridInput : MonoBehaviour, IDragHandler, IPointerClickHandler {

        public float LerpedValue {
            get {
                return Mathf.Lerp(minValue, maxValue, sliderValue);
            }
            set {
                sliderValue = Mathf.InverseLerp(minValue, maxValue, value);
            }
        }
        public float minValue = 0;
        public float maxValue = 1;
        public bool modValue;

        public delegate string UIFormatFunction(float val);
        public UIFormatFunction formatFunction;
        
        private float sliderValue;
        float realWidth;
        UIProgressBar bar;
        InputField field;
        RectTransform rectTf;

        public event Action OnValueUpdated;

        private void Awake() {
            bar = GetComponentInChildren<UIProgressBar>();
            field = GetComponentInChildren<InputField>();
            rectTf = transform as RectTransform;
            realWidth = GetComponentInParent<Canvas>().scaleFactor * rectTf.rect.width;

            field.onEndEdit.AddListener(DisableInputField);
            OnValueUpdated += Redraw;
        }
        private void Start() {
            field.enabled = false;
        }

        public void OnDrag(PointerEventData eventData) {
            float barStart = transform.position.x - realWidth / 2;
            if (modValue) 
                sliderValue = ((eventData.position.x - barStart) / realWidth + 100) % 1;
            else 
                sliderValue = Mathf.Clamp01((eventData.position.x - barStart) / realWidth);
            OnValueUpdated();
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (!eventData.dragging) {
                field.enabled = true;
                field.Select();
            }
        }

        public void SetValue(float lerpedVal) {
            LerpedValue = lerpedVal;
            Redraw();
        }

        private void DisableInputField(string val) {
            if (float.TryParse(val, out float lerpedVal))
                LerpedValue = lerpedVal;
            field.enabled = false;
            OnValueUpdated();
        }

        private void Redraw() {
            bar.SetFill(sliderValue);
            field.SetTextWithoutNotify(formatFunction(LerpedValue));
        }
    }
}
