using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReefEditor.ContentLoading;
using ReefEditor.VoxelEditing;

namespace ReefEditor.UI {
    public class UIMaterialsWindow : UIWindow {
        private GameObject matIconPrefab;
        private List<UIBlocktypeIconDisplay> icons;

        private SNContentLoader loader;
        private bool iconsSpawned = false;

        void Start() {
            if (matIconPrefab == null)
                matIconPrefab = Resources.Load<GameObject>("UI Material Icon");
            loader = EditorManager.GetContentLoader();
        }

        public override void EnableWindow() {
            base.EnableWindow();
            transform.GetChild(1).GetChild(0).GetComponent<Text>().text = $"Load {(EditorManager.BelowZero ? "BZ" : "SN")} materials";
        }

        public void LoadMaterials() {

            if (!EditorManager.CheckIsGamePathValid()) {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).gameObject.SetActive(true);

            if (!EditorManager.GetContentLoader().IsFinished()) {
                EditorManager.instance.OnContentLoaded += DisplayMaterialIcons;
                EditorManager.InitiateMaterialsLoad();
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

        private void DisplayMaterialIcons() {
            if (iconsSpawned) return;
            icons = new List<UIBlocktypeIconDisplay>();

            foreach(BlocktypeMaterial mat in loader.blocktypesData) {
                if (mat != null && mat.ExistsInGame) {
                    GameObject newIconGameObj = Instantiate(matIconPrefab, transform.GetChild(2).GetChild(0).GetChild(0).GetChild(0));
                    UIBlocktypeIconDisplay newicon = new UIBlocktypeIconDisplay(newIconGameObj, mat);
                    icons.Add(newicon);
                }
            }
            ResizeContent();
            UpdateIconVisibility();
            iconsSpawned = true;
        }

        private bool IsIconVisible(RectTransform rectTf) {
            return rectTf.position.y > 0 & rectTf.position.y < 1080;
        } 

        private class UIBlocktypeIconDisplay {
            public GameObject gameObject;
            BlocktypeMaterial mat;
            bool isVisible = false;

            public UIBlocktypeIconDisplay(GameObject _gameObject, BlocktypeMaterial _mat) {
                gameObject = _gameObject;
                mat = _mat;

                string materialName = mat.prettyName;
                if (materialName.Contains("deco")) {
                    materialName = string.Concat(materialName.Split(' ')[0], "-deco");
                }

                string title = $"{mat.blocktype}) {materialName}";
                gameObject.GetComponentInChildren<Text>().text = title;

                gameObject.GetComponent<Button>().onClick.AddListener(OnMaterialSelected);
            }    

            public void UpdateVisibility(bool newVisible) {

                bool changed = newVisible != isVisible;
                if (!changed) return;
                isVisible = newVisible;

                if (isVisible) {
                    if (mat.SideTexture != null) {
                        gameObject.GetComponent<Image>().sprite = ProcessCombinedTexture(mat.MainTexture, mat.SideTexture, 2);
                    } else {
                        gameObject.GetComponent<Image>().sprite = ProcessTexture(mat.MainTexture, 2);
                    }
                } else {
                    gameObject.GetComponent<Image>().sprite = null;
                }
            }

            private Sprite ProcessTexture(Texture2D ogTex, int downsamples) {
                var colors = ogTex.GetPixels();

                int dsWidth = ogTex.width >> downsamples;
                int dsHeight = ogTex.height >> downsamples;
                var newColors = new Color[dsHeight * dsWidth];

                for (int x = 0; x < dsWidth; ++x) {
                    int xOg = x << downsamples;
                    for (int y = 0; y < dsHeight; ++y) {
                        int yOg = y << downsamples;
                        var color = colors[xOg + yOg * ogTex.width];
                        color.a = 1;
                        newColors[x + y * dsWidth] = color;
                    }
                }

                Texture2D downsampled = new Texture2D(dsWidth, dsHeight);
                downsampled.SetPixels(newColors);
                downsampled.Apply();
                var sprite = Sprite.Create(downsampled, new Rect(0, 0, dsWidth, dsHeight), Vector2.one * 0.5f, dsWidth);
                return sprite;
            }
            private Sprite ProcessCombinedTexture(Texture2D ogTexTop, Texture2D ogTexBottom, int downsamples) {

                var colorsTop = ogTexTop.GetPixels();
                var colorsBottom = ogTexBottom.GetPixels();

                int dsWidth = Mathf.Min(ogTexTop.width, ogTexBottom.width) >> downsamples;
                int dsHeight = Mathf.Min(ogTexTop.height, ogTexBottom.height) >> downsamples;
                var newColors = new Color[dsHeight * dsWidth];

                int widthFactorBottom = ogTexBottom.width / dsWidth,    heightFactorBottom = ogTexBottom.height / dsHeight;
                int widthFactorTop =    ogTexTop.width / dsWidth,       heightFactorTop = ogTexTop.height / dsHeight;

                int halfpoint = dsHeight / 2;

                for (int x = 0; x < dsWidth; ++x) {
                    int xOg = x * widthFactorBottom;
                    for (int y = 0; y < halfpoint; ++y) {
                        int yOg = y * heightFactorBottom;
                        var color = colorsBottom[xOg + yOg * ogTexBottom.width];
                        color.a = 1;
                        newColors[x + y * dsWidth] = color;
                    }

                    xOg = x * widthFactorTop;
                    for (int y = halfpoint; y < dsHeight; ++y) {
                        int yOg = y * heightFactorTop;
                        var color = colorsTop[xOg + yOg * ogTexTop.width];
                        color.a = 1;
                        newColors[x + y * dsWidth] = color;
                    }
                }

                Texture2D downsampled = new Texture2D(dsWidth, dsHeight);
                downsampled.SetPixels(newColors);
                downsampled.Apply();
                var sprite = Sprite.Create(downsampled, new Rect(0, 0, dsWidth, dsHeight), Vector2.one * 0.5f, dsWidth);
                return sprite;
            }

            public void OnMaterialSelected() {
                VoxelMetaspace.instance.brushMaster.SetBrushBlocktype((byte)mat.blocktype);
            }
        }
    }
}