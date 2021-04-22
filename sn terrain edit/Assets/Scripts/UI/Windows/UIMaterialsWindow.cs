using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UIMaterialsWindow : UIWindow
{
    GameObject matIconPrefab;
    bool materialsLoaded = false;
    List<UIBlocktypeIconDisplay> icons;

    public override void EnableWindow() {
        base.EnableWindow();
        if (matIconPrefab == null)
            matIconPrefab = Resources.Load<GameObject>("UI Material Icon");
    }

    public void LoadMaterials() {
        transform.GetChild(1).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(true);

        if (!materialsLoaded) {
            StartCoroutine(DisplayMaterialIcons());
        }
    }

    public void UpdateIconVisibility() {
        foreach (UIBlocktypeIconDisplay icon in icons) {
            bool visible = IsIconVisible(icon.gameObject.transform as RectTransform);
            icon.UpdateVisibility(visible);
        }
    }

    void ResizeContent() {
        (transform.GetChild(2).GetChild(0).GetChild(0) as RectTransform).offsetMin = new Vector2(0, -225 * Mathf.Ceil(transform.GetChild(2).GetChild(0).GetChild(0).GetChild(0).childCount / 2f));
    }

    IEnumerator DisplayMaterialIcons() {
        yield return null;
        yield return StartCoroutine(SNContentLoader.instance.LoadContent());
        materialsLoaded = true;
        icons = new List<UIBlocktypeIconDisplay>();

        foreach(BlocktypeMaterial mat in SNContentLoader.instance.blocktypesData) {
            if (mat != null && mat.ExistsInGame) {
                GameObject newIconGameObj = Instantiate(matIconPrefab, transform.GetChild(2).GetChild(0).GetChild(0).GetChild(0));
                UIBlocktypeIconDisplay newicon = new UIBlocktypeIconDisplay(newIconGameObj, mat);
                icons.Add(newicon);
            }
        }
        ResizeContent();
        UpdateIconVisibility();
    }

    bool IsIconVisible(RectTransform rectTf) {
        RectTransform scrollViewTf = transform.GetChild(2).GetChild(0) as RectTransform;
        return rectTf.position.y > 0 & rectTf.position.y < 1080;
    } 

    class UIBlocktypeIconDisplay {
        public GameObject gameObject;
        BlocktypeMaterial mat;
        bool isVisible = false;

        public UIBlocktypeIconDisplay(GameObject instance, BlocktypeMaterial mat) {
            this.gameObject = instance;
            this.mat = mat;

            string materialName = mat.trueName;
            if (materialName.Contains("deco")) {
                materialName = string.Concat(materialName.Split(' ')[0], "-deco");
            }

            string title = $"{mat.blocktype.ToString()}) {materialName}";
            gameObject.GetComponentInChildren<Text>().text = title;

            instance.GetComponent<Button>().onClick.AddListener(OnMaterialSelected);
        }    

        public void UpdateVisibility(bool newVisible) {

            bool changed = newVisible != this.isVisible;
            if (!changed) return;
            this.isVisible = newVisible;

            if (isVisible) {
                if (mat.MainTexture != null) {
                    Texture2D tex1 = mat.MainTexture;

                    Color[] colors = tex1.GetPixels();
                    for (int i = 0; i < colors.Length; i++) {
                        colors[i].a = 1;
                    }
                    Texture2D texNoAlpha = new Texture2D(tex1.width, tex1.height);
                    texNoAlpha.SetPixels(colors);
                    texNoAlpha.Apply();

                    Sprite sprite = Sprite.Create(texNoAlpha, new Rect(0.0f, 0.0f, tex1.width, tex1.height), new Vector2(0.5f, 0.5f), tex1.width);
                    gameObject.GetComponent<Image>().sprite = sprite;
                }
                if (mat.SideTexture != null) {
                    Texture2D tex2 = mat.SideTexture;

                    Color[] colors = tex2.GetPixels();
                    for (int i = 0; i < colors.Length; i++) {
                        colors[i].a = 1;
                    }
                    Texture2D tex2NoAlpha = new Texture2D(tex2.width, tex2.height);
                    tex2NoAlpha.SetPixels(colors);
                    tex2NoAlpha.Apply();

                    Sprite sprite = Sprite.Create(tex2NoAlpha, new Rect(0.0f, 0.0f, tex2.width, tex2.height), new Vector2(0.5f, 0.5f), tex2.width);
                    gameObject.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = sprite;
                } else {
                    gameObject.transform.GetChild(0).gameObject.SetActive(false);
                }
            } else {
                gameObject.GetComponent<Image>().sprite = null;
            }
        }

        public void OnMaterialSelected() {
            Brush.SetBrushMaterial((byte)mat.blocktype);
        }
    }
}
