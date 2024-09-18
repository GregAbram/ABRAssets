using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics.Tracing;
using UnityEngine.UIElements;
using UnityEngine.AI;

public class CameraModel : MonoBehaviour
{
    public int button = 0;
    public float mouseRotationSensitivity = .1f;
    public float mouseMovementSensitivity = 2f;
    protected Vector3 lastPosition;
    protected bool mouseIsDown = false;
    protected bool saveState = false;
    protected bool moved = false;
    string cameraFile = "";

    TiledDisplayManager tdm = null;

    [Serializable] 
    class CState
    {
        public Vector3 P;
        public Quaternion R;
    }

    [Serializable]
    public class CurrentView
    {
        public float px, py, pz, rx, ry, rz, rw;
    }

    void SaveState()
    {
        CState cState = new CState();

        cState.P = transform.position;
        cState.R = transform.rotation;

        var json = JsonUtility.ToJson(cState);

        if (File.Exists(cameraFile))
        {
            var fn = File.AppendText(cameraFile);
            fn.Write(json);
            fn.Write("\n");
            fn.Close();
        }
        else
        {
            var fn = File.CreateText(cameraFile);
            fn.Write(json);
            fn.Write("\n");
            fn.Close();
        }
    }

    public virtual void Start()
    {
        tdm = ScriptableObject.CreateInstance<TiledDisplayManager>();
        if (! tdm.IsMaster())
        {   
            float l, r, t, b;
            tdm.GetOffset(out l, out r, out b, out t); 

            float ws = tdm.GetWallScaling();

            Camera cam = GetComponent<Camera>(); 
            cam.projectionMatrix = PerspectiveOffCenter(ws * l, ws * r, ws * b, ws * t, cam.nearClipPlane, cam.farClipPlane);
            return;
        }

        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        string s;
        if (cfg.GetString("-mouseRotationSensitivity", out s))
        {
            mouseRotationSensitivity = Convert.ToSingle(s);
        }

        if (cfg.GetString("-mouseMovementSensitivity", out s))
        {
            mouseMovementSensitivity = Convert.ToSingle(s);
        }
        
        if (! cfg.GetString("-transformCache", out s))
            s = ".";

        cameraFile = string.Format("{0}/camera", s);

        if (File.Exists(cameraFile))
        {
            string[] transforms = System.IO.File.ReadAllLines(cameraFile);

            if (transforms.Length > 0)
            {
                string json = transforms[transforms.Length - 1];
                CState cState = JsonUtility.FromJson<CState>(json);

                transform.position = cState.P;
                transform.rotation = cState.R;
            } 
        }
    }

    public virtual void CameraController()
    {
        if (Input.GetMouseButtonDown(button))
        {
            lastPosition = Input.mousePosition;
            mouseIsDown = true;
            moved = false;
            saveState = false;
        }
        else if (mouseIsDown)
        {
            moved = true;

            Vector3 currentPosition = Input.mousePosition;
            Vector3 deltaPosition = currentPosition - lastPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;

            Quaternion q_r, q_u;

            q_r = Quaternion.AngleAxis(inputY, Camera.main.transform.right);
            q_u = Quaternion.AngleAxis(-inputX, Camera.main.transform.up);

            transform.rotation = q_r * q_u * transform.rotation;
        }
            
        float inputSW = Input.GetAxis("Mouse ScrollWheel") * mouseMovementSensitivity;
        if (inputSW != 0)
        {
            transform.position += inputSW * transform.forward;
            moved = true;
        }

        if (Input.GetMouseButtonUp(button))
        {
            mouseIsDown = false;
            saveState = moved;
        }
    }

    void Update()
    {
        if (tdm.IsMaster())
        {        
            CameraController();

            if (saveState)
            {
                SaveState();
                saveState = false;
            }

            if (tdm.NumberOfTiles() > 0)
            {
                CurrentView cv = new CurrentView();
                var fmt = new BinaryFormatter();
                var ms = new MemoryStream();
            
                Vector3 p;
                Quaternion r;
                transform.GetPositionAndRotation(out p, out r);

                cv.px = p.x;
                cv.py = p.y;
                cv.pz = p.z;
                cv.rx = r.x;
                cv.ry = r.y;
                cv.rz = r.z;
                cv.rw = r.w;

                fmt.Serialize(ms, cv);
                byte[] message = ms.ToArray();

                tdm.Communicate(ref message);
            }
        }
        else
        {
            CurrentView cv;
            var ms = new MemoryStream();
            var fmt = new BinaryFormatter();

            byte[] message = null;
            tdm.Communicate(ref message);

            ms = new MemoryStream(message);
            cv = (CurrentView)fmt.Deserialize(ms);

            //Debug.Log("Camera received " + cv.px + " " + cv.py + " " + cv.pz + " " + cv.rx + " " + cv.ry + " " + cv.rz + " " + cv.rw);


            Vector3 p = new Vector3(cv.px, cv.py, cv.pz);
            Quaternion r = new Quaternion(cv.rx, cv.ry, cv.rz, cv.rw);

            transform.SetPositionAndRotation(p, r);
        }
    }

    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        //Debug.Log("OC " + left + " " + right + " " + bottom + " " + top);
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }


}

