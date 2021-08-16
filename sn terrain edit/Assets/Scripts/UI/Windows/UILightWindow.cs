using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UILightWindow : UIWindow {
        // Two things:
        // 1. Rotate the sun around Y axis with a hybrid slider
        UIHybridInput rotationSlider;
        public Transform sunTransform;
        // 2. Enable/disable brush light
        UICheckbox checkbox;
        Light brushLight;

        private void Start() {
            rotationSlider = GetComponentInChildren<UIHybridInput>();
            rotationSlider.OnValueUpdated += UpdateSunRotation;
            rotationSlider.formatFunction = FormatAngle;
            rotationSlider.SetValue(250);

            brushLight = Brush.GetBrushLight();
            checkbox = GetComponentInChildren<UICheckbox>();
            checkbox.transform.GetComponent<Button>().onClick.AddListener(UpdateBrushLight);

            UpdateSunRotation();
            UpdateBrushLight();
        }

        private string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)} deg";

        // getting commands from UI
        void UpdateBrushLight() => brushLight.enabled = checkbox.check;
        void UpdateSunRotation() => sunTransform.eulerAngles = new Vector3(60, rotationSlider.LerpedValue, 0);
    }
}