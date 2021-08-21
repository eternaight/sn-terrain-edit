using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UILightWindow : UIWindow {
        // Two things:
        // 1. Rotate the sun around X and Y axis with a hybrid slider
        UIHybridInput rotationXSlider;
        UIHybridInput rotationYSlider;
        public Transform sunTransform;
        // 2. Enable/disable brush light
        UICheckbox checkbox;
        Light brushLight;

        private void Start() {
            rotationXSlider = transform.GetChild(1).GetChild(0).GetComponentInChildren<UIHybridInput>();
            rotationXSlider.OnValueUpdated += UpdateSunRotation;
            rotationXSlider.formatFunction = FormatAngle;
            rotationXSlider.maxValue = 90;
            rotationXSlider.SetValue(60);

            rotationYSlider = transform.GetChild(1).GetChild(1).GetComponentInChildren<UIHybridInput>();
            rotationYSlider.OnValueUpdated += UpdateSunRotation;
            rotationYSlider.formatFunction = FormatAngle;
            rotationYSlider.maxValue = 360;
            rotationYSlider.SetValue(250);
            rotationYSlider.modValue = true;

            brushLight = Brush.GetBrushLight();
            checkbox = GetComponentInChildren<UICheckbox>();
            checkbox.transform.GetComponent<Button>().onClick.AddListener(UpdateBrushLight);

            UpdateSunRotation();
            UpdateBrushLight();
        }

        private string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)} deg";

        // getting commands from UI
        void UpdateBrushLight() => brushLight.enabled = checkbox.check;
        void UpdateSunRotation() => sunTransform.eulerAngles = new Vector3(rotationXSlider.LerpedValue, rotationYSlider.LerpedValue, 0);
    }
}