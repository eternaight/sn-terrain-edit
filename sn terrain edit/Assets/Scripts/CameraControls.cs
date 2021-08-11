using UnityEngine;
using UnityEngine.EventSystems;

namespace ReefEditor {
    public class CameraControls : MonoBehaviour {
        Camera cam;
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
            cam = Camera.main;
            brush = GetComponent<Brush>();
        }

        void OnRegionLoad() {
            moveLock = false;
            zoomLevel = 0.25f;
            transform.parent.rotation = Quaternion.Euler(new Vector3(30, -135, 0));
            cam.transform.parent.position = (VoxelWorld.end - VoxelWorld.start + Vector3.one) * VoxelWorld.OCTREE_SIDE * 5 / 2;
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
                        
                        zoomLevel = Mathf.Clamp01(zoomLevel - Input.mouseScrollDelta.y * 0.01f);
                        transform.localPosition = new Vector3(0, 0, zoomStart + zoomRange * zoomLevel);
                    }
                }
            }
        }

        public bool IsMouseOverUI() {
            return EventSystem.current.IsPointerOverGameObject();
        }

        Vector3 GetMoveVector() {
            return new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Lateral"), Input.GetAxis("Vertical"));
        }

        void Move() {

            speed = Input.GetKey(KeyCode.LeftShift) ? 20 : 10;

            // first-person movement
            transform.parent.Translate(GetMoveVector() * speed * Time.deltaTime);

            transform.parent.position = CapPosition(transform.parent.position);
        }

        Vector3 CapPosition(Vector3 pos) {
            
            // TODO: remove dependancy
            Vector3 regionEnd = VoxelWorld.end;
            Vector3 regionStart = VoxelWorld.start;

            float cappedPosX = Mathf.Clamp(pos.x, 0, 160 * (regionEnd.x - regionStart.x + 1));
            float cappedPosY = Mathf.Clamp(pos.y, 0, 160 * (regionEnd.y - regionStart.y + 1));
            float cappedPosZ = Mathf.Clamp(pos.z, 0, 160 * (regionEnd.z - regionStart.z + 1));
            return new Vector3(cappedPosX, cappedPosY, cappedPosZ);
        }
    }
}