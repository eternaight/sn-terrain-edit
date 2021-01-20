﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brush : MonoBehaviour
{
    public static float brushSize = 0;
    public static float minBrushSize = 1f;
    public static float maxBrushSize = 64;
    public static byte selectedType;
    public static BrushMode mode;

    // Action timings
    float lastBrushTime;
    int brushStreakAmount = 0;
    float brushActionPeriod = 1.0f;

    [SerializeField] GameObject brushAreaObject;

    void Start() {
        CreateBrushObject();
    }

    public void BrushAction(bool doAction) {
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        Physics.Raycast(ray, out hit, Mathf.Infinity, 1);

        if (hit.collider) {
            DrawBrushSphere(hit.point);

            if (doAction) {

                brushStreakAmount++;
                if (DoNextBrushStroke()) {

                    lastBrushTime = Time.time;

                    if (hit.collider.gameObject.GetComponentInParent<VoxelandMesh>()) {
                        hit.collider.gameObject.GetComponentInParent<VoxelandMesh>().DensityAction_Sphere(hit.point, Brush.brushSize, mode);
                    }
                }
            } else {
                brushStreakAmount = 0;
            }
        } else {
            DisableBrushGizmo();
        }
        
    }
    void CreateBrushObject() {
        brushAreaObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        brushAreaObject.GetComponent<MeshRenderer>().sharedMaterial = Globals.get.brushGizmoMat;
        brushAreaObject.GetComponent<SphereCollider>().enabled = false;
        brushAreaObject.transform.localScale = Vector3.one * Brush.brushSize;
    }
    public void DrawBrushSphere(Vector3 position) {

        brushAreaObject.SetActive(true);
        brushAreaObject.transform.position = position;
        brushAreaObject.transform.localScale = Vector3.one * 2 * Brush.brushSize;
    }
    public void DisableBrushGizmo() {
        brushAreaObject.SetActive(false);
    }

    public static void SetBrushMaterial(byte value) {
        Brush.selectedType = value;
    }
    public static void SetBrushSize(float t) {
        brushSize = Mathf.Lerp(minBrushSize, maxBrushSize, t);
    }
    public static void SetBrushMode(BrushMode newMode) {
        mode = newMode;

        Globals.get.brushGizmoMat.color = Globals.get.brushColors[(int)mode];
    }

    bool DoNextBrushStroke() {
        return (Time.time - lastBrushTime) >= (brushActionPeriod / (2 * Mathf.Clamp(brushStreakAmount, 1, 5)));
    }
}

public enum BrushMode {
    Add,
    Remove,
    Paint,
    Eyedropper
}
