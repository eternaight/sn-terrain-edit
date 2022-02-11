using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UILoadWindow : UIWindow {
        public void LoadBatch() {

            if (!EditorManager.CheckIsGamePathValid()) {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }

            InputField rangeStartInput = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<InputField>();
            InputField rangeEndInput = transform.GetChild(2).GetChild(2).GetChild(2).GetComponent<InputField>();

            Vector3Int start, end;
            bool startEntered = TryParseBatchString(rangeStartInput.text, out start);
            bool endEntered = TryParseBatchString(rangeEndInput.text, out end);

            if (!startEntered && !endEntered) {
                EditorUI.DisplayErrorMessage("Please enter at least one batch index: \n\"x(space)y(space)z\"");
                return;
            }
            
            // assume user wants to load a single batch if only 1 is correct
            if (!startEntered) start = end; 
            if (!endEntered) end = start;

            VoxelMetaspace.InitiateRegionLoad(start, end);
            base.DisableWindow();
        } 

        private bool TryParseBatchString(string s, out Vector3Int index) {
            
            string[] splitString = s.Split(' ');
            index = Vector3Int.zero;
            if (splitString.Length != 3) {
                return false;
            }

            int x, y, z;
            if (int.TryParse(splitString[0], out x) && int.TryParse(splitString[1], out y) && int.TryParse(splitString[2], out z)) {
                index = new Vector3Int(x, y, z);
                return true;
            }
            
            return false;
        }
    }
}