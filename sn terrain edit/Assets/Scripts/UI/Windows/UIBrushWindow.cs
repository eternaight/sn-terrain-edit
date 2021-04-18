using UnityEngine;
using UnityEngine.UI;

public class UIBrushWindow : UIWindow
{
    UIButtonSelect modeSelector;

    public override void Start() {
        UpdateBrushRadius();

        modeSelector = transform.GetChild(1).GetChild(1).GetComponent<UIButtonSelect>();
        modeSelector.OnValueChanged += UpdateBrushMode;
    }
    public void UpdateBrushRadius() {

        float sliderValue = transform.GetChild(1).GetChild(3).GetChild(1).GetComponent<Slider>().value;
        float transformed = Mathf.Sqrt(sliderValue);

        Brush.SetBrushSize(sliderValue);
        UpdateRadiusDisplay();
    }
    public void UpdateRadiusDisplay() {
        string displayValue = System.Math.Round(Brush.brushSize, 1).ToString("0.0");
        transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<Text>().text = displayValue;
    }

    public void UpdateBrushBlocktype() {

        string typeString = transform.GetChild(1).GetChild(5).GetComponent<InputField>().text;
        
        byte typeValue = 0;
        if (byte.TryParse(typeString, out typeValue)) {
            Brush.SetBrushMaterial(typeValue);
        }
        UpdateBlocktypeDisplay();
    }
    public void UpdateBlocktypeDisplay() {
        transform.GetChild(1).GetChild(5).GetComponent<InputField>().text = Brush.selectedType.ToString();
    }

    public void UpdateBrushMode() {
        Brush.mode = (BrushMode)(modeSelector.selection);
    }

    // overrides
    public override void EnableWindow()
    {
        base.EnableWindow();
        UpdateRadiusDisplay();
        UpdateBlocktypeDisplay();
    }
    public override void DisableWindow()
    {
        base.DisableWindow();
    }
}
