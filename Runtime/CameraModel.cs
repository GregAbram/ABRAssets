
using System;

using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class CameraModel : MonoBehaviour
{
    public string message_tag = "camera model";
    public string cache_name = "world";
    public int button = 0;
    public char modifier = 'n'; // n = none, c = ctrl, a = alt, s = shift

    public float mouseRotationSensitivity = .1f;
    public float mouseMovementSensitivity = 2f;

#if ENABLE_INPUT_SYSTEM
    protected Vector2 lastPosition;
#else
    protected Vector3 lastPosition;
#endif

    protected bool mouseIsDown = false;
    string cameraFile = "camera";
    Configurator cfg = null;

    TiledDisplayManager tdm = null;

    bool cacheCamera = false;

    Vector3 setPosition;
    Quaternion setRotation;


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

    protected void SaveState()
    {
        if (! cacheCamera)
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
        catch
        {
            Debug.Log("Cannot create or write camera transform history file " + cameraFile);
        }
    }

    static bool first = true;

    public virtual void Start()
    {
        cfg = ScriptableObject.CreateInstance<Configurator>();

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
        else if (first)
        {
            first = false;
            Camera cam = GetComponent<Camera>(); 
        }

        if (cfg.GetString("-cameraCache", out cameraFile))
        {
            string envDir;
            if (!cfg.GetString("-ABRConfig", out envDir))
                envDir = Environment.GetEnvironmentVariable("ABRConfig");

            if (envDir == null)
                envDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            cacheCamera = true;
            cameraFile = string.Format("{0}/{1}", envDir, cameraFile);

            if (File.Exists(cameraFile))
            {
                string[] transforms = System.IO.File.ReadAllLines(cameraFile);

                if (transforms.Length > 0)
                {
                    string json = transforms[transforms.Length - 1];
                    CState cState = JsonUtility.FromJson<CState>(json);

                    transform.position = cState.P;
                    transform.rotation = cState.R;

                    setPosition = cState.P;
                    setRotation = cState.R;
                }
            }
        }

        string s;
           
        if (cfg.GetString("-mouseMovementSensitivity", out s))
        {
            mouseMovementSensitivity = Convert.ToSingle(s);
        }

        if (cfg.GetString("-mouseRotationSensitivity", out s))
        {
            mouseRotationSensitivity = Convert.ToSingle(s);
        }

        transform.GetPositionAndRotation(out setPosition, out setRotation);
    }

    public virtual bool CameraController()
    {
        bool save = false;

#if ENABLE_INPUT_SYSTEM
        bool b = false;
        switch (button)
        {
            case 0: b =  Mouse.current.leftButton.wasPressedThisFrame; break;
            case 1: b =  Mouse.current.rightButton.wasPressedThisFrame; break;
            case 2: b =  Mouse.current.middleButton.wasPressedThisFrame; break;
        }

        bool c = Keyboard.current.ctrlKey.isPressed;
        bool a = Keyboard.current.altKey.isPressed;
        bool s = Keyboard.current.shiftKey.isPressed;        

        if (b && ((modifier == 'n' && !c && !a && !s) || (modifier == 'c' && c) || (modifier == 'a' && a) || (modifier == 's' && s)))
        {
            mouseIsDown = true;
            lastPosition = Mouse.current.position.ReadValue();
        }
        
        if (!b && ((modifier == 'n' && !c && !a && !s) || (modifier == 'c' && c) || (modifier == 'a' && a) || (modifier == 's' && s)))
        { 
            switch (button)
            {
                case 0: b =  Mouse.current.leftButton.wasReleasedThisFrame; break;
                case 1: b =  Mouse.current.rightButton.wasReleasedThisFrame; break;
                case 2: b =  Mouse.current.middleButton.wasReleasedThisFrame; break;
            }

            if (b)
                mouseIsDown = false;
        }  
#else        
        bool b = Input.GetMouseButton(button);
        bool c = Input.GetKey(KeyCode.LeftControl);
        bool a = Input.GetKey(KeyCode.LeftAlt);
        bool s = Input.GetKey(KeyCode.LeftShift);        
        
        if (!b && mouseIsDown)
        {
            mouseIsDown = false;
            return true;
        }
        else if (b && ((modifier == 'n' && !c && !a && !s) || (modifier == 'c' && c) || (modifier == 'a' && a) || (modifier == 's' && s)))
        {
            if (! mouseIsDown)
            {
                lastPosition =  Input.mousePosition;
                mouseIsDown = true;
            }
        }
#endif

        if (mouseIsDown)
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 currentPosition = Mouse.current.position.ReadValue();
            Vector2 deltaPosition = currentPosition - lastPosition;
            lastPosition = currentPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;
#else
            Vector3 currentPosition = Input.mousePosition;
            Vector3 deltaPosition = currentPosition - lastPosition;
            lastPosition = currentPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;
#endif
            if (inputX != 0 || inputY != 0)
            {
                Quaternion q_r, q_u;

                q_r = Quaternion.AngleAxis(inputY, Camera.main.transform.right);
                q_u = Quaternion.AngleAxis(-inputX, Camera.main.transform.up);

                transform.rotation = q_r * q_u * transform.rotation;    
                save = true;  
            }        
        }
            
#if ENABLE_INPUT_SYSTEM
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        float inputSW = scroll.y;
#else
        float inputSW = Input.GetAxis("Mouse ScrollWheel") * mouseMovementSensitivity;
#endif

        if (inputSW != 0)
        {
            transform.position += inputSW * transform.forward;
            save = true;
        }


        return save;
    }

    void Update()
    {
#if true
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;
#endif
        if (tdm.IsMaster())
        {
            if (Keyboard.current.sKey.wasPressedThisFrame) { Debug.Log("S pressed"); }

#if false
            if (Input.GetKeyDown(KeyCode.S))
            {
                transform.GetPositionAndRotation(out setPosition, out setRotation);
                return;
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                transform.position = setPosition;
                transform.rotation = setRotation;
                return;
            }
#endif


            bool saveState = CameraController();
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

                tdm.messageManager.SendMessage(message_tag, message);
#if false
                Debug.LogFormat("CAM {0} {1} {2} {3} {4} {5} {6}", p.x, p.y, p.z, r.x, r.y, r.z, r.w);
#endif
            }
        }
        else
        {
            byte[] currentViewMessage = tdm.messageManager.GetMessage(message_tag);
            if (currentViewMessage != null)
            {
                var ms = new MemoryStream(currentViewMessage);
                var fmt = new BinaryFormatter();
                CurrentView cv = (CurrentView)fmt.Deserialize(ms);

                Vector3 p = new Vector3(cv.px, cv.py, cv.pz);
                Quaternion r = new Quaternion(cv.rx, cv.ry, cv.rz, cv.rw);
#if false

                Debug.LogFormat("CAM {0} {1} {2} {3} {4} {5} {6}", p.x, p.y, p.z, r.x, r.y, r.z, r.w);
#endif
                transform.SetPositionAndRotation(p, r);
            }
        }
    }

    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        Debug.Log("OC " + left + " " + right + " " + bottom + " " + top);
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
