using TMPro;
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
        
        Brush.SetBrushSize(sliderValue);

        string displayValue = System.Math.Round(Brush.brushSize, 1).ToString("0.0");
        transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = displayValue;
    }

    public void UpdateBrushMode() {
        Brush.mode = (BrushMode)(modeSelector.selection);
    }

    // overrides
    public override void EnableWindow()
    {
        base.EnableWindow();
    }
    public override void DisableWindow()
    {
        base.DisableWindow();
    }
}
