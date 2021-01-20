﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

public class SaveData : MonoBehaviour
{
    static SaveData save;

    SavedPaths data;

    void Awake() {
        save = this;
        // read from save
        Load();

        Globals.SetBatchInputPath(data.GetValue(Globals.sourcePathKey), false);
        Globals.SetBatchOutputPath(data.GetValue(Globals.outputPathKey), false);
    }

    void Start() {
    }

    public static void WriteKey(string key, string value) {
        
        save.data.Write(key, value);
        Save();
    }
    public static string GetValue(string key) {
        if (save.data.ContainsKey(key) != -1) 
            return save.data.GetValue(key);
        return "";
    }

    static void Save() {

        XmlSerializer xml = new XmlSerializer(typeof(SavedPaths));
        FileStream stream = new FileStream(Application.persistentDataPath + "/data.xml", FileMode.Create);
        xml.Serialize(stream, save.data);
        stream.Close();
    }

    static void Load() {

        bool pathSaveExists = File.Exists(Application.persistentDataPath + "/data.xml");
        if (pathSaveExists) {
            XmlSerializer xml = new XmlSerializer(typeof(SavedPaths));
            FileStream stream = new FileStream(Application.persistentDataPath + "/data.xml", FileMode.Open);
            save.data = xml.Deserialize(stream) as SavedPaths;
            stream.Close();
        } else {
            save.data = new SavedPaths();
        }
    }

    
    [System.Serializable]
    public class SavedPaths {
        public List<string> keys;
        public List<string> values;

        public SavedPaths() {
            keys = new List<string>();
            values = new List<string>();
        }

        public void Write(string key, string value) {
            
            if (keys.Contains(key)) {
                values[ContainsKey(key)] = value;
            } else {
                keys.Add(key);
                values.Add(value);
            }
        }

        public string GetValue(string key) {
            
            int i = ContainsKey(key);

            if (i != -1) {
                return values[i];
            }
            return "";
        }

        public int ContainsKey(string key) {
            int i = 0;
            bool keyFound = false;
            for (i = 0; i < keys.Count; i++) {
                if (keys[i] == key) {
                    keyFound = true;
                    break;
                }
            }

            return keyFound ? i : -1;
        }
    }
}

