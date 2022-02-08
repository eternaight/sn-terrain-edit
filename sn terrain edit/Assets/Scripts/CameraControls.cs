using UnityEngine;
using UnityEngine.EventSystems;

namespace ReefEditor {
    public class CameraControls : MonoBehaviour {
        Vector3 dragStartPos = Vector3.zero;
        Vector3 prevRotation;

        public bool moveLock = true;

        public bool dragging;
        public float sensitivity;
        float zoomLevel;
        float zoomRange = -1000;
        float zoomStart = -1;
        public float speed = 2;
        bool mouseOverUI;
        Brush brush;

        void Start() {
            brush = GetComponent<Brush>();
        }

        void OnRegionLoad() {
            moveLock = false;
            zoomLevel = 0.25f;
            transform.parent.rotation = Quaternion.Euler(new Vector3(30, -135, 0));
            PoseCamera();
        }

        public void PoseCamera() {
            Camera.main.transform.parent.position = (Vector3)VoxelWorld.RealSize() * 0.5f;
        }

        void Update() {

            if (!moveLock) {

                Move();
                mouseOverUI = IsMouseOverUI();

                // rotating cam
                dragging = Input.GetMouseButton(2);
                if (dragging) {
                    if (dragStartPos == Vector3.zero) {
                        dragStartPos = Input.mousePosition;
                    }

                    Vector3 newrotation = (Input.mousePosition - dragStartPos) * sensitivity;
                    newrotation = new Vector3(-newrotation.y, newrotation.x, 0);

                    transform.parent.rotation = Quaternion.Euler(prevRotation + newrotation);
                } else {
                    dragStartPos = Vector2.zero;
                    prevRotation = transform.parent.rotation.eulerAngles;
                }

                if (brush.enabled) {
                    if (mouseOverUI) {
                        brush.DisableBrushGizmo();
                    }
                    else {
                        brush.BrushAction(Input.GetMouseButton(0));
                    }
                }

                if (!mouseOverUI) {
                    zoomLevel = Mathf.Clamp01(zoomLevel - Input.mouseScrollDelta.y * 0.01f);
                    transform.localPosition = new Vector3(0, 0, zoomStart + zoomRange * zoomLevel);
                }
            }
        }

        bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();
        Vector3 GetMoveVector() => new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Lateral"), Input.GetAxis("Vertical"));

        private void Move() {

            speed = Input.GetKey(KeyCode.LeftShift) ? 20 : 10;

            // first-person movement
            transform.parent.Translate(GetMoveVector() * speed * Time.deltaTime);

            transform.parent.position = CapPosition(transform.parent.position);
        }

        private Vector3 CapPosition(Vector3 pos) {
            var size = VoxelWorld.RealSize();

            float cappedPosX = Mathf.Clamp(pos.x, 0, size.x);
            float cappedPosY = Mathf.Clamp(pos.y, 0, size.y);
            float cappedPosZ = Mathf.Clamp(pos.z, 0, size.z);
            return new Vector3(cappedPosX, cappedPosY, cappedPosZ);
        }
    }
}