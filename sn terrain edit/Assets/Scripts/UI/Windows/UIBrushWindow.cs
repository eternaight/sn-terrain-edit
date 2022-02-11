using ReefEditor.VoxelEditing;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIBrushWindow : UIWindow {
        
        private UIButtonSelect modeSelector;
        private UIHybridInput brushSizeSelector;
        private UIHybridInput brushStrengthSelector;
        private InputField blocktypeInput;
        private BrushMaster brushMaster;

        private bool inited;

        private void Initialize() {

            inited = true;
            brushMaster = VoxelMetaspace.instance.brushMaster;

            modeSelector = transform.GetChild(1).GetChild(0).GetComponentInChildren<UIButtonSelect>();
            modeSelector.OnSelectionChanged += SetNewBrushMode;

            brushSizeSelector = transform.GetChild(1).GetChild(1).GetComponentInChildren<UIHybridInput>();
            brushSizeSelector.minValue = 1;
            brushSizeSelector.maxValue = 32;
            brushSizeSelector.formatFunction = FormatSize;
            brushSizeSelector.OnValueUpdated += SetNewBrushSize;

            brushStrengthSelector = transform.GetChild(1).GetChild(2).GetComponentInChildren<UIHybridInput>();
            brushStrengthSelector.formatFunction = FormatStrength;
            brushStrengthSelector.OnValueUpdated += SetNewBrushStrength;

            blocktypeInput = transform.GetChild(1).GetChild(3).GetComponentInChildren<InputField>();
            brushMaster.OnParametersChanged += RedrawValues;
        }

        public override void EnableWindow()
        {
            base.EnableWindow();
            if (!inited) {
                Initialize();
            }

            RedrawValues();
            brushMaster.BrushWindowActive = true;
        }
        public override void DisableWindow()
        {
            base.DisableWindow();
            brushMaster.BrushWindowActive = false;
        }

        // For receiving commands from UI
        public void SetNewBrushSize() {
            brushMaster.SetBrushSize(brushSizeSelector.LerpedValue);
        }
        public void SetNewBrushStrength() {
            brushMaster.SetBrushStrength(brushStrengthSelector.LerpedValue);
        }
        public void SetNewBrushMode() {
            brushMaster.SetBrushMode(modeSelector.selection);
        }
        public void SetNewBlocktype() {
            if (byte.TryParse(blocktypeInput.text, out byte typeValue)) {
                if (VoxelMetaspace.instance.CheckBlocktypeDefined(typeValue)) {
                    brushMaster.SetBrushBlocktype(typeValue);
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
        private void RedrawBrushMode() {
            modeSelector.SetSelectionFromBrushUpdate((int)brushMaster.userSelectedMode);
        }
        private void RedrawRadiusDisplay() {
            brushSizeSelector.SetValue(brushMaster.brush.radius);
        }
        private void RedrawStrengthDisplay() {
            //brushStrengthSelector.SetValue(0);
        }
        private void RedrawBlocktypeDisplay() {
            blocktypeInput.SetTextWithoutNotify(brushMaster.brush.selectedBlocktype.ToString());
        }
    }
}
