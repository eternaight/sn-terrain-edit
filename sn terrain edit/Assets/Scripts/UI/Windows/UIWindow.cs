using UnityEditor;
using UnityEngine;

public class UIWindow : MonoBehaviour
{
    string title = "WindowTitle";
    bool windowActive = false;

    public virtual void Start() {
        
    }

    // [MenuItem("GameObject/Extra UI/UI Window", false, 10)]
    // static void CreateCustomGameObject(MenuCommand menuCommand)
    // {
    //     // Create a custom game object
    //     GameObject go = Instantiate((Resources.Load("UIWindow") as GameObject));

    //     // Ensure it gets reparented if this was a context click (otherwise does nothing)
    //     GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
    //     // Register the creation in the undo system
    //     Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
    //     Selection.activeObject = go;
    // }

    public virtual void DisableWindow() {
        windowActive = false;
        gameObject.SetActive(windowActive);
    }
    public virtual void EnableWindow() {
        windowActive = true;
        PushToTop();
        gameObject.SetActive(windowActive);
    }
    public void ToggleWindow() {
        if (windowActive) DisableWindow();
        else EnableWindow();
    }

    public void PushToTop() {
        transform.SetAsLastSibling();
    }
}
