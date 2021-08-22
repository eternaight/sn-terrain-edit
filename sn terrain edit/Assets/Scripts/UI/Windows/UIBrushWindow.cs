using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIBrushWindow : UIWindow {
        UIButtonSelect modeSelector;
        UIHybridInput brushSizeSelector;
        UIHybridInput brushStrengthSelector;
        InputField blocktypeInput;

        void Awake() {
            modeSelector = transform.GetChild(1).GetChild(0).GetComponentInChildren<UIButtonSelect>();
            modeSelector.OnSelectionChanged += SetNewBrushMode;

            brushSizeSelector = transform.GetChild(1).GetChild(1).GetComponentInChildren<UIHybridInput>();
            brushSizeSelector.minValue = Brush.minBrushSize;
            brushSizeSelector.maxValue = Brush.maxBrushSize;
            brushSizeSelector.formatFunction = FormatSize;
            brushSizeSelector.OnValueUpdated += SetNewBrushSize;

            brushStrengthSelector = transform.GetChild(1).GetChild(2).GetComponentInChildren<UIHybridInput>();
            brushStrengthSelector.formatFunction = FormatStrength;
            brushStrengthSelector.OnValueUpdated += SetNewBrushStrength;

            blocktypeInput = transform.GetChild(1).GetChild(3).GetComponentInChildren<InputField>();
            Brush.OnParametersChanged += RedrawValues;
        }

        public override void EnableWindow()
        {
            Brush.SetEnabled(true);
            base.EnableWindow();
            RedrawValues();
        }
        public override void DisableWindow()
        {
            Brush.SetEnabled(false);
            base.DisableWindow();
        }

        // For receiving commands from UI
        public void SetNewBrushSize() {
            Brush.SetBrushSize(brushSizeSelector.LerpedValue);
        }
        public void SetNewBrushStrength() {
            Brush.SetBrushStrength(brushStrengthSelector.LerpedValue);
        }
        public void SetNewBrushMode() {
            Brush.SetBrushMode(modeSelector.selection);
        }
        public void SetNewBlocktype() {
            if (byte.TryParse(blocktypeInput.text, out byte typeValue)) {
                if (ContentLoading.SNContentLoader.instance.blocktypesData[typeValue].ExistsInGame) {
                    Brush.SetBrushMaterial(typeValue);
                }
            }
        }

        // formatting
        public string FormatSize(float val) => System.Math.Round(val, 1).ToString("0.0");
        public string FormatStrength(float val) => System.Math.Round(val, 3).ToString("0.000");


        // For redrawing UI
        public void RedrawValues() {
            RedrawBrushMode();
            RedrawBlocktypeDisplay();
            RedrawRadiusDisplay();
            RedrawStrengthDisplay();
        }
        void RedrawBrushMode() {
            modeSelector.SetSelectionFromBrushUpdate((int)Brush.activeMode);
        }
        public void RedrawRadiusDisplay() {
            brushSizeSelector.SetValue(Brush.brushSize);
        }
        public void RedrawStrengthDisplay() {
            brushStrengthSelector.SetValue(Brush.brushStrength);
        }
        public void RedrawBlocktypeDisplay() {
            blocktypeInput.SetTextWithoutNotify(Brush.selectedType.ToString());
        }
    }
}
