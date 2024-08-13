using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics.Tracing;
using TMPro;

public class TransformCache : MonoBehaviour
{
    public string root = "camera";
    static string transformCache;
    public int button = 1;
    string filename = "";
    private float last_scrollwheel = 0;

    [Serializable]
    public class Xform
    {
        public float px, py, pz, rx, ry, rz, rw;
    }

    void Start()
    {
        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();
        if (! cfg.GetString("-transformCache", out transformCache))
            transformCache = ".";

        Debug.Log("Using Transform Cache: " + transformCache);

        filename = string.Format("{0}/{1}", transformCache, root);

        if (File.Exists(filename))
        {
            string[] transforms = System.IO.File.ReadAllLines(filename);
            if (transforms.Length > 0)
            {
                string json = transforms[transforms.Length - 1];
                Xform xform = JsonUtility.FromJson<Xform>(json);

                Vector3 p = new Vector3(xform.px, xform.py, xform.pz);
                Quaternion r = new Quaternion(xform.rx, xform.ry, xform.rz, xform.rw);

                transform.SetPositionAndRotation(p, r);
            }  
        }
        else
        {
            Debug.Log("Transform file: " + filename + " does not exist initially");
        }
        
    }

    void LateUpdate()
    {

        if (Input.GetMouseButtonUp(button) || Input.GetKeyDown(KeyCode.C) || (Input.GetAxis("Mouse ScrollWheel") == 0 && last_scrollwheel != 0))
        {
            Vector3 p; Quaternion r;
            transform.GetPositionAndRotation(out p, out r);

            Xform xform = new Xform();
            xform.px = p.x;
            xform.py = p.y;
            xform.pz = p.z;
            xform.rx = r.x;
            xform.ry = r.y;
            xform.rz = r.z;
            xform.rw = r.w;

            var json = JsonUtility.ToJson(xform);

            if (File.Exists(filename))
            {
                var fn = File.AppendText(filename);
                fn.Write(json);
                fn.Write("\n");
                fn.Close();
            }
            else
            {
                var fn = File.CreateText(filename);
                fn.Write(json);
                fn.Write("\n");
                fn.Close();
            }

            Debug.Log("Wrote " + filename);
        }

        last_scrollwheel = Input.GetAxis("Mouse ScrollWheel");
    }
}
