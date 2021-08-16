using System;
using UnityEngine;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class Brush : MonoBehaviour {
        public static float brushSize = 10;
        public static float minBrushSize = 1;
        public static float maxBrushSize = 32;
        public static byte selectedType = 11;
        public static float brushStrength = 0.5f;
        public static BrushMode userSelectedMode = BrushMode.Add;
        public static BrushMode activeMode { get; private set; }
        public static event Action OnParametersChanged;

        BrushStroke stroke;
        const float brushActionPeriod = 1.0f;

        GameObject brushAreaObject;

        void Start() {
            if (brushAreaObject == null) {
                CreateBrushObject();
                DisableBrushGizmo();
            }
        }
        void OnDisable() {
            DisableBrushGizmo();
        }

        void CreateBrushObject() {
            brushAreaObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brushAreaObject.GetComponent<MeshRenderer>().sharedMaterial = Globals.instance.brushGizmoMat;
            brushAreaObject.GetComponent<SphereCollider>().enabled = false;
            brushAreaObject.transform.localScale = Vector3.one * brushSize;

            GameObject lightObj = new GameObject("brushLight");
            lightObj.transform.SetParent(brushAreaObject.transform);
            lightObj.transform.localScale = Vector3.one;
            Light light = lightObj.AddComponent<Light>();
            light.enabled = false;
            light.intensity = Mathf.Clamp(Mathf.Sqrt(brushSize), 1, 12);
            light.range = 2 * brushSize;
        }

        public static Light GetBrushLight() {
            Brush brush = FindObjectOfType<Brush>();
            if (brush.brushAreaObject == null) {
                brush.CreateBrushObject();
                brush.DisableBrushGizmo();
            }
            return brush.brushAreaObject.GetComponentInChildren<Light>();
        }

        void Update() {
            // apply modifiers
            BrushMode _newActiveMode = userSelectedMode;
            if (Input.GetKey(KeyCode.LeftShift)) {
                // Always smooth
                _newActiveMode = BrushMode.Smooth;
            } else if (Input.GetKey(KeyCode.LeftControl)) {
                // Complementary op
                if (userSelectedMode == BrushMode.Add)
                    _newActiveMode = BrushMode.Remove;
                if (userSelectedMode == BrushMode.Paint)
                    _newActiveMode = BrushMode.Eyedropper;
                if (userSelectedMode == BrushMode.Remove)
                    _newActiveMode = BrushMode.Add;
                if (userSelectedMode == BrushMode.Eyedropper)
                    _newActiveMode = BrushMode.Paint;
            }
            if (_newActiveMode != activeMode) {
                activeMode = _newActiveMode;
                OnParametersChanged();
            }
        }


        public void BrushAction(bool doAction) {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, 1);

            if (hit.collider) {
                DrawBrushGizmo(hit.point, hit.normal);
                if (doAction) {
                    VoxelMesh mesh = hit.collider.gameObject.GetComponentInParent<VoxelMesh>();
                    if (mesh) {
                        if (activeMode == BrushMode.Eyedropper) {
                            SetBrushMaterial(VoxelWorld.SampleBlocktype(hit.point, ray));
                        } else {
                            if (stroke.ReadyForNextAction()) {

                                if (stroke.strokeLength == 0) stroke.FirstStroke(hit.point, hit.normal, brushSize, brushStrength, activeMode);
                                else stroke.ContinueStroke(hit.point, activeMode);

                                VoxelMetaspace.metaspace.ApplyDensityAction(stroke);
                            }
                        }
                    }
                } else {
                    stroke.EndStroke();
                }
            } else {
                DisableBrushGizmo();
            }
            
        }
        public void DrawBrushGizmo(Vector3 position, Vector3 normal) {

            brushAreaObject.SetActive(true);
            brushAreaObject.transform.position = position;
            brushAreaObject.transform.GetChild(0).position = position + normal * 2;

            if (activeMode == BrushMode.Eyedropper) {
                brushAreaObject.transform.localScale = Vector3.one * 2;
            } else {
                brushAreaObject.transform.localScale = Vector3.one * 2 * brushSize;
            }
        }
        public void DisableBrushGizmo() {
            if (brushAreaObject)
                brushAreaObject.SetActive(false);
        }
        public GameObject GetBrushObject() {
            return brushAreaObject;
        }

        public static void SetBrushMaterial(byte value) {
            selectedType = value;
            OnParametersChanged?.Invoke();
        }
        public static void SetBrushSize(float t) {
            brushSize = t;
            Light light = Camera.main.GetComponent<Brush>().brushAreaObject.GetComponentInChildren<Light>();
            light.intensity = Mathf.Clamp(Mathf.Sqrt(brushSize), 1, 12); ;
            light.range = 2 * brushSize;
            OnParametersChanged?.Invoke();
        }
        public static void SetBrushStrength(float t) {
            brushStrength = t;
            OnParametersChanged?.Invoke();
        }
        public static void SetBrushMode(int selection) {
            userSelectedMode = (BrushMode)selection;
            if (selection < Globals.instance.brushColors.Length)
                Globals.instance.brushGizmoMat.color = Globals.instance.brushColors[selection];
            OnParametersChanged?.Invoke();
        }

        public static void SetEnabled(bool enable) {
            Camera.main.GetComponent<Brush>().enabled = enable;
        }

        public struct BrushStroke {
            public Vector3 brushLocation;
            public float brushRadius;
            float strength;
            public BrushMode brushMode;
            public Vector3 firstStrokePoint;
            public Vector3 firstStrokeNormal;
            public int strokeLength;
            float lastBrushTime;

            // Stroke frequency increases with more strokes
            public void FirstStroke(Vector3 _position, Vector3 _normal, float _radius, float _strength, BrushMode _mode) {
                strokeLength = 1;

                brushLocation = _position;
                firstStrokePoint = _position;
                firstStrokeNormal = _normal;
                brushRadius = _radius;
                brushMode = _mode;
                strength = _strength;
                
                lastBrushTime = Time.time;
            }
            public void ContinueStroke(Vector3 newPos, BrushMode newMode) {
                strokeLength++;

                brushLocation = newPos;
                brushMode = newMode;

                lastBrushTime = Time.time;
            }
            public void EndStroke() {
                strokeLength = 0;

                brushLocation = Vector3.zero;
                firstStrokePoint = Vector3.zero;
                firstStrokeNormal = Vector3.zero;
                
                lastBrushTime = 0;
            }

            // smooth values
            private static float SmoothStep(float t, float min = 0, float max = 1) {
                t = Mathf.Clamp01((t - min) / (max - min));
                return t * t * (3 - 2 * t);
            }
            public float GetWeight(Vector3 voxelPos) {
                // float brushWeight = SmoothStep((brushLocation - voxelPos).magnitude, brushRadius * 0.7f, brushRadius);
                return strength * 252;
            }

            public bool ReadyForNextAction() {
                return (Time.time - lastBrushTime) >= (brushActionPeriod / (2 * Mathf.Clamp(strokeLength, 1, 5)));
            }
        }
    }

    /* Brush types:
    add/remove
    paint material / select material?
    flatten surface
    smooth available always with SHIFT */
    public enum BrushMode {
        Add,
        Remove,
        Paint,
        Eyedropper,
        Flatten,
        Smooth,
    }
}