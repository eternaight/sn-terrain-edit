using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIBrushWindow : UIWindow {
        UIButtonSelect modeSelector;

        public void Awake() {
            Brush.SetBrushSize(0);
            Brush.SetBrushMode(0);
            Brush.SetBrushMaterial(11);
            Brush.OnParametersChanged += RedrawValues;
        }
        public override void Start() {
            modeSelector = transform.GetChild(1).GetChild(1).GetComponent<UIButtonSelect>();
            modeSelector.OnSelectionChanged += SetNewBrushMode;
        }

        public override void EnableWindow()
        {
            base.EnableWindow();
            RedrawValues();
        }

        // For receiving commands from UI
        public void SetNewBrushSize() {

            float sliderValue = transform.GetChild(1).GetChild(3).GetChild(1).GetComponent<Slider>().value;
            float transformed = Mathf.Sqrt(sliderValue);

            Brush.SetBrushSize(sliderValue);
        }
        public void SetNewBrushMode() {
            Brush.SetBrushMode(modeSelector.selection);
        }
        public void SetNewBlocktype() {

            string typeString = transform.GetChild(1).GetChild(5).GetComponent<InputField>().text;
            
            byte typeValue = 0;
            if (byte.TryParse(typeString, out typeValue)) {
                Brush.SetBrushMaterial(typeValue);
            }
        }


        // For redrawing UI
        public void RedrawValues() {
            RedrawBlocktypeDisplay();
            RedrawRadiusDisplay();
        }
        public void RedrawRadiusDisplay() {
            string displayValue = System.Math.Round(Brush.brushSize, 1).ToString("0.0");
            transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<Text>().text = displayValue;
        }
        public void RedrawBlocktypeDisplay() {
            transform.GetChild(1).GetChild(5).GetComponent<InputField>().text = Brush.selectedType.ToString();
        }
    }
}
