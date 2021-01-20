using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] RectTransform windowTf;
    Vector3 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        UIWindow window = GetComponentInParent<UIWindow>();
        window.PushToTop();

        offset = Input.mousePosition - windowTf.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 pos = Input.mousePosition - offset;
        pos = new Vector3(Mathf.Clamp(pos.x, 0, Screen.width), Mathf.Clamp(pos.y, 0, Screen.height), 0);
        windowTf.position = pos;
    }
}
