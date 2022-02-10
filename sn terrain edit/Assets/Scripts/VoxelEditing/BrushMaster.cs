using System;
using UnityEngine;

namespace ReefEditor.VoxelEditing {
    public class BrushMaster {

        public bool BrushWindowActive { get; set; }
        public readonly Brush brush;
        public BrushMode userSelectedMode = BrushMode.Add;

        private GameObject brushGizmoObject;

        public event Action OnParametersChanged;

        public BrushMaster() {
            brush = new Brush();
        }
        public void Start() {
            CreateGizmo();
        }

        private void CreateGizmo() {
            brushGizmoObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brushGizmoObject.GetComponent<MeshRenderer>().sharedMaterial = Globals.instance.brushGizmoMat;
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

            Globals.UpdateBoundaries(position, brush.radius + 2);
        }
        public void DisableBrushGizmo() {
            brushGizmoObject.SetActive(false);
            Globals.UpdateBoundaries(Vector3.zero, 0);
        }
        public Light GetLightComponent() => brushGizmoObject.transform.GetChild(0).GetComponent<Light>();

        private BrushMode ApplyModeModifiers(BrushMode mode, bool shift, bool ctrl) {
            if (shift) {
                // Always smooth
                return BrushMode.Smooth;
            }
            if (ctrl) {
                // Complementary op
                return (BrushMode)((int)mode + 1 - (int)(mode) % 2);
            }
            return mode;
        }

        public void BrushAction(RaycastHit hit, Ray ray, bool shift, bool ctrl) {
            var activeMode = ApplyModeModifiers(userSelectedMode, shift, ctrl);
            if (activeMode == BrushMode.Eyedropper) {
                SetBrushBlocktype(VoxelMetaspace.instance.SampleBlocktype(hit.point, ray));
            } else {
                brush.TryStroke(hit);
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
        }
        public void SetBrushMode(int selection) {
            userSelectedMode = (BrushMode)selection;
            if (selection < Globals.instance.brushColors.Length)
                Globals.instance.brushGizmoMat.color = Globals.instance.brushColors[selection];
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
            public byte selectedBlocktype;
            public float radius;
            public BrushMode mode;

            // variables
            private Vector3 position;
            private Vector3 strokeOrigin, strokeNormal;
            private float lastStrokeTime;
            private int strokeLength;

            private const float BRUSH_STROKE_PERIOD = 1;

            public bool GetMask(Vector3Int voxel) {
                return Vector3.Distance(voxel, InVoxelSpace(position)) < radius;
            }
            private Vector3 InVoxelSpace(Vector3 p) => VoxelMetaspace.instance.transform.InverseTransformPoint(p);
            public OctNodeData BlendVoxel(OctNodeData data, Vector3Int voxel) {
                switch (mode) {
                    case BrushMode.Add:
                        return VoxelOps.VoxelAddSmooth(data, BrushDensity(voxel), selectedBlocktype);
                    case BrushMode.Remove:
                        return VoxelOps.VoxelAddSmooth(data, 252 - BrushDensity(voxel), selectedBlocktype);
                    case BrushMode.Paint:
                        return new OctNodeData(selectedBlocktype, data.density);
                    case BrushMode.Flatten:
                        return VoxelOps.VoxelFlatten(voxel, strokeOrigin, strokeNormal, selectedBlocktype);
                    case BrushMode.Smooth:
                        return VoxelOps.VoxelSmooth(data, voxel, selectedBlocktype, 2);
                    default:
                        return new OctNodeData();
                };
            }
            public Vector3Int[] GetBounds() {
                var a = InVoxelSpace(position);
                var voxelPos = new Vector3Int((int)a.x, (int)a.y, (int)a.z);
                return new Vector3Int[] {
                    voxelPos - Vector3Int.one * Mathf.CeilToInt(radius),
                    voxelPos + Vector3Int.one * Mathf.CeilToInt(radius)
                };
            }

            public void TryStroke(RaycastHit hit) {

                if (!ReadyForNextStroke()) return;

                if (strokeLength == 0) {
                    strokeOrigin = hit.point;
                    strokeNormal = hit.normal;
                }

                position = hit.point;
                lastStrokeTime = Time.time;
                strokeLength++;

                VoxelMetaspace.instance.ApplyVoxelGrid(this);
            }
            public void ResetStroke() {
                lastStrokeTime = 0;
                strokeLength = 0;
            }

            private byte BrushDensity(Vector3Int voxel) {
                float signedDist = radius - Vector3.Distance(voxel, InVoxelSpace(position));
                return OctNodeData.EncodeDensity(signedDist);
            }

            private bool ReadyForNextStroke() {
                return (Time.time - lastStrokeTime) >= (BRUSH_STROKE_PERIOD / (2 * Mathf.Clamp(strokeLength, 1, 5)));
            }
        }
    }
}