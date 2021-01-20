using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctNodeObject : MonoBehaviour
{
    public bool displayNode;
    public int type;

    public void Setup(Vector3 vector, float halfside, int type, ref System.Action onBatchEnable) {

        displayNode = true;

        gameObject.transform.localPosition = vector + Vector3.one * halfside;
        gameObject.transform.localScale = new Vector3(halfside * 2, halfside * 2, halfside * 2);
        this.type = type;
        gameObject.GetComponent<MeshRenderer>().material.color = Globals.ColorFromType(type);

        if (!displayNode) {
            Destroy(this.gameObject);
        } else {
            GetComponent<MeshRenderer>().enabled = false;
            onBatchEnable += SetVisible;
        }
    }

    void SetVisible() {
        GetComponent<MeshRenderer>().enabled = true;
    }
}
