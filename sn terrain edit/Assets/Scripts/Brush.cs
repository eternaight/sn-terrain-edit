using System;
using UnityEngine;
using ReefEditor.VoxelTech;

namespace ReefEditor {
    public class Brush : MonoBehaviour {
        public static float brushSize = 0;
        public static float minBrushSize = 1f;
        public static float maxBrushSize = 64;
        public static byte selectedType;
        public static BrushMode mode;
        public static event Action OnParametersChanged;

        BrushStroke stroke;
        const float brushActionPeriod = 1.0f;

        GameObject brushAreaObject;

        void Start() {
            CreateBrushObject();
        }
        void OnRegionLoad() {
            SetBrushMaterial(11);
        }

        void CreateBrushObject() {
            brushAreaObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brushAreaObject.GetComponent<MeshRenderer>().sharedMaterial = Globals.instance.brushGizmoMat;
            brushAreaObject.GetComponent<SphereCollider>().enabled = false;
            brushAreaObject.transform.localScale = Vector3.one * Brush.brushSize;
        }

        public void BrushAction(bool doAction) {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, 1);

            if (hit.collider) {
                DrawBrushGizmo(hit.point);
                if (doAction) {
                    VoxelMesh mesh = hit.collider.gameObject.GetComponentInParent<VoxelMesh>();
                    if (mesh) {
                    
                        // apply modifiers
                        BrushMode actionMode = mode;
                        if (Input.GetKey(KeyCode.LeftShift)) {
                            // Always smooth
                            actionMode = BrushMode.Smooth;
                        }
                        else if (Input.GetKey(KeyCode.LeftControl)) {
                            // Complementary op
                            if (mode == BrushMode.Add)
                                actionMode = BrushMode.Remove;
                            if (mode == BrushMode.Paint)
                                actionMode = BrushMode.Eyedropper;
                        }

                        if (actionMode == BrushMode.Eyedropper) {
                            SetBrushMaterial(mesh.SampleBlocktype(hit.point, ray));
                        } else {
                            if (stroke.ReadyForNextAction()) {

                                if (stroke.strokeLength == 0) stroke.FirstStroke(hit.point, hit.normal, brushSize, actionMode);
                                else stroke.ContinueStroke(hit.point, actionMode);

                                mesh.DensityAction_Sphere(stroke);
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
        public void DrawBrushGizmo(Vector3 position) {

            brushAreaObject.SetActive(true);
            brushAreaObject.transform.position = position;

            if (mode == BrushMode.Eyedropper) {
                brushAreaObject.transform.localScale = Vector3.one * 2;
            } else {
                brushAreaObject.transform.localScale = Vector3.one * 2 * Brush.brushSize;
            }
        }
        public void DisableBrushGizmo() {
            brushAreaObject.SetActive(false);
        }

        public static void SetBrushMaterial(byte value) {
            Brush.selectedType = value;
            Brush.OnParametersChanged?.Invoke();
        }
        public static void SetBrushSize(float t) {
            brushSize = Mathf.Lerp(minBrushSize, maxBrushSize, t);
            Brush.OnParametersChanged?.Invoke();
        }
        public static void SetBrushMode(int selection) {
            mode = (BrushMode)selection;
            Globals.instance.brushGizmoMat.color = Globals.instance.brushColors[selection];
            Brush.OnParametersChanged?.Invoke();
        }

        public struct BrushStroke {
            public Vector3 brushLocation;
            public float brushRadius;
            public BrushMode brushMode;
            public Vector3 firstStrokePoint;
            public Vector3 firstStrokeNormal;
            public int strokeLength;
            float lastBrushTime;

            // Stroke frequency increases with more strokes
            public void FirstStroke(Vector3 _position, Vector3 _normal, float _radius, BrushMode _mode) {
                strokeLength = 1;

                brushLocation = _position;
                firstStrokePoint = _position;
                firstStrokeNormal = _normal;
                brushRadius = _radius;
                brushMode = _mode;
                
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
        Paint,
        Flatten,
        Remove = 10,
        Eyedropper,
        Smooth = 100,
    }
}