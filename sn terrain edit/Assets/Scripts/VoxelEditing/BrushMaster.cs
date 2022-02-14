using System;
using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public class BrushMaster {

        public bool BrushWindowActive { get; set; }
        public readonly Brush brush;
        public BrushMode userSelectedMode = BrushMode.Add;

        private GameObject brushGizmoObject;
        private Color[] brushModesPalette;

        public event Action OnParametersChanged;

        public BrushMaster() {
            brush = new Brush();
        }
        public void Start() {
            CreateGizmo();
            brushModesPalette = EditorManager.GetBrushModesPalette();
        }

        private void CreateGizmo() {
            brushGizmoObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brushGizmoObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Materials/GizmoBrush");
            brushGizmoObject.GetComponent<SphereCollider>().enabled = false;
            brushGizmoObject.SetActive(false);

            GameObject lightObj = new GameObject("brushLight");
            lightObj.transform.SetParent(brushGizmoObject.transform);
            lightObj.transform.localScale = Vector3.one;
            Light light = lightObj.AddComponent<Light>();
            light.enabled = false;

            UpdateGizmoAppearance();
        }
        private void UpdateGizmoAppearance() {
            brushGizmoObject.transform.localScale = Vector3.one * brush.radius;
            var light = brushGizmoObject.transform.GetChild(0).GetComponent<Light>();
            light.intensity = Mathf.Clamp(Mathf.Sqrt(brush.radius), 1, 12);
            light.range = 2 * brush.radius;
        }
        public void DrawBrushGizmo(Vector3 position, Vector3 normal) {

            brushGizmoObject.SetActive(true);
            brushGizmoObject.transform.position = position;
            brushGizmoObject.transform.GetChild(0).position = position + normal * 2;

            if (userSelectedMode == BrushMode.Eyedropper) {
                brushGizmoObject.transform.localScale = Vector3.one * 2;
            } else {
                brushGizmoObject.transform.localScale = Vector3.one * 2 * brush.radius;
            }

            EditorManager.UpdateBoundaryMaterial(position, brush.radius + 2);
        }
        public void DisableBrushGizmo() {
            brushGizmoObject.SetActive(false);
            EditorManager.UpdateBoundaryMaterial(Vector3.zero, 0);
        }
        public Light GetLightComponent() => brushGizmoObject.transform.GetChild(0).GetComponent<Light>();

        private BrushMode ApplyModeModifiers(BrushMode mode, bool shift, bool ctrl) {
            if (shift) {
                // Always smooth
                return BrushMode.Smooth;
            }
            if (ctrl) {
                // Complementary op
                return (BrushMode)((int)mode % 2 == 1 ? (int)mode - 1 : (int)mode + 1); 
            }
            return mode;
        }

        public void BrushAction(RaycastHit hit, Ray ray, bool shift, bool ctrl) {
            var activeMode = ApplyModeModifiers(userSelectedMode, shift, ctrl);

            if (activeMode == BrushMode.Eyedropper) {
                SetBrushBlocktype(VoxelMetaspace.instance.SampleBlocktype(hit.point, ray));
            } else {
                brush.TryStroke(hit, activeMode);
            }
        }
        public void BrushStop() {
            brush.ResetStroke();
            DisableBrushGizmo();
        }

        public void SetBrushBlocktype(byte value) {
            brush.selectedBlocktype = value;
            OnParametersChanged?.Invoke();
        }
        public void SetBrushSize(float t) {
            brush.radius = t;
            UpdateGizmoAppearance();
            OnParametersChanged?.Invoke();
        }
        public void SetBrushStrength(float t) {
            brush.strength = t;
            OnParametersChanged?.Invoke();
        }
        public void SetBrushMode(int selection) {
            userSelectedMode = (BrushMode)selection;
            if (selection < brushModesPalette.Length)
                brushGizmoObject.GetComponent<MeshRenderer>().sharedMaterial.color = brushModesPalette[selection];
            OnParametersChanged?.Invoke();
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

        public class Brush : IVoxelGrid {

            // user settings
            public byte selectedBlocktype = 1;
            public float radius;
            public float strength;
            public BrushMode mode;

            // variables
            private Vector3 position;
            private Vector3 strokeOrigin, strokeNormal;
            private float lastStrokeTime;
            private int strokeLength;

            private const float BRUSH_STROKE_PERIOD = 1;

            public bool GetMask(Vector3Int voxel) {
                return Vector3.Distance(voxel, InVoxelSpace(position)) <= radius;
            }
            private Vector3 InVoxelSpace(Vector3 p) => VoxelMetaspace.instance.transform.InverseTransformPoint(p);
            public void BlendVoxel(VoxelData data, Vector3Int voxel) {
                switch (mode) {
                    case BrushMode.Add:
                        VoxelOps.VoxelUnion(data, BrushDensity(voxel), selectedBlocktype);
                        break;
                    case BrushMode.Remove:
                        VoxelOps.VoxelSubtract(data, BrushDensity(voxel), selectedBlocktype);
                        break;
                    case BrushMode.Paint:
                        VoxelOps.VoxelPaint(data, selectedBlocktype);
                        break;
                    case BrushMode.Flatten:
                        VoxelOps.VoxelFlatten(data, voxel, strokeOrigin, strokeNormal, selectedBlocktype);
                        break;
                    case BrushMode.Smooth:
                        VoxelOps.VoxelSmooth(data, voxel, selectedBlocktype, 2);
                        break;
                    default:
                        break;
                };
            }
            public Vector3Int[] GetBounds() {
                var a = InVoxelSpace(position);
                var voxelPos = new Vector3Int((int)a.x, (int)a.y, (int)a.z);
                var diagonal = Vector3Int.one * Mathf.CeilToInt(radius * 1.41f);

                return new Vector3Int[] {
                    voxelPos - diagonal,
                    voxelPos + diagonal
                };
            }

            public void TryStroke(RaycastHit hit, BrushMode mode) {

                if (!ReadyForNextStroke()) return;

                if (strokeLength == 0) {
                    strokeOrigin = hit.point;
                    strokeNormal = hit.normal;
                }

                this.mode = mode;
                position = hit.point;
                lastStrokeTime = Time.time;
                strokeLength++;

                VoxelMetaspace.instance.ApplyVoxelGrid(this);
            }
            public void ResetStroke() {
                lastStrokeTime = 0;
                strokeLength = 0;
            }

            private float BrushDensity(Vector3Int voxel) {
                return radius - Vector3.Distance(InVoxelSpace(position), voxel);
            }

            private bool ReadyForNextStroke() {
                return (Time.time - lastStrokeTime) >= (BRUSH_STROKE_PERIOD / (2 * Mathf.Clamp(strokeLength, 1, 5)));
            }
        }
    }
}