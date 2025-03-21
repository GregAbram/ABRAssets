using System;

using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;


public class CameraModel : MonoBehaviour
{
    public string message_tag = "camera model";
    public string cache_name = "world";
    public int button = 0;
    public float mouseRotationSensitivity = .1f;
    public float mouseMovementSensitivity = 2f;
    protected Vector3 lastPosition;
    protected bool mouseIsDown = false;
    string cameraFile = "camera";

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
        if (cameraFile == "")
            return;
            
        CState cState = new CState();

        cState.P = transform.position;
        cState.R = transform.rotation;

        var json = JsonUtility.ToJson(cState);

        try
        {
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
        catch (Exception e)
        {
            Debug.Log("Cannot create or write camera transform history file " + cameraFile);
        }
    }

    public virtual void Start()
    {
        tdm = TiledDisplayManager.Instance;
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
        
        if (cfg.GetString("-transformCache", out s))
        {
            if (! Directory.Exists(s))
            {
                Debug.LogFormat("Transforms directory {0} does not exist", s);
                cameraFile = "";
            }
            else
            {
                try
                {
                    cameraFile = string.Format("{0}/{1}", s, cameraFile);
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
                catch (Exception e)
                {
                    Debug.LogFormat("Unable to create  transform file {0}/{1}", s, cameraFile));
                    cameraFile = "";
                }
            }
        } 
    }

    public virtual bool CameraController()
    {
        bool save = false;

        if (Input.GetMouseButtonDown(button))
        {
            lastPosition = Input.mousePosition;
            mouseIsDown = true;

        }
        else if (mouseIsDown)
        {
            Vector3 currentPosition = Input.mousePosition;
            Vector3 deltaPosition = currentPosition - lastPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;

            if (inputX != 0 || inputY != 0)
            {
                Quaternion q_r, q_u;

                q_r = Quaternion.AngleAxis(inputY, Camera.main.transform.right);
                q_u = Quaternion.AngleAxis(-inputX, Camera.main.transform.up);

                transform.rotation = q_r * q_u * transform.rotation;              
            }
        }
            
        float inputSW = Input.GetAxis("Mouse ScrollWheel") * mouseMovementSensitivity;
        if (inputSW != 0)
        {
            transform.position += inputSW * transform.forward;
            save = true;
        }

        if (Input.GetMouseButtonUp(button))
        {
            mouseIsDown = false;
            save = true;
        }

        return save;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (tdm.IsMaster())
        {               
            bool saveState = CameraController();
            if (saveState)
            {
                SaveState();
                saveState = false;

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

                    tdm.messageManager.SendMessage(message_tag, message);
                    Debug.LogFormat("CAM {0} {1} {2} {4} {5} {6} {7}", p.x, p.y, p.z, r.x, r.y, r.z, r.w);
                }
            }
        }
        else
        {
            Debug.Log("CMU s");
            byte[] currentViewMessage = tdm.messageManager.GetMessage(message_tag);
            if (currentViewMessage != null)
            {
                var ms = new MemoryStream(currentViewMessage);
                var fmt = new BinaryFormatter();
                CurrentView cv = (CurrentView)fmt.Deserialize(ms);

                Vector3 p = new Vector3(cv.px, cv.py, cv.pz);
                Quaternion r = new Quaternion(cv.rx, cv.ry, cv.rz, cv.rw);

                transform.SetPositionAndRotation(p, r);
            }
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

