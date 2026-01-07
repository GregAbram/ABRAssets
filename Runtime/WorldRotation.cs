using System;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.EventSystems;
using IVLab.ABREngine;


public class WorldRotation : MonoBehaviour
{
    public string message_tag = "world rotation";
    public int button = 1;
    public char modifier = 'n'; // n = none, c = ctrl, a = alt, s = shift

    public float worldRotationSensitivity = 2f;
    Vector3 lastPosition;
    bool mouseIsDown = false;
    public string worldFile = "world"; 

    Camera mainCamera;

    TiledDisplayManager tdm = null;

    bool cacheXform = false;

    string transformCache;

    [Serializable]
    public class WorldTransform
    {
        public float px, py, pz, rx, ry, rz, rw;
    }
 	class WorldState
    {
        public Vector3 P;
        public Quaternion Q;
    }

    void Start()
    {
        tdm = TiledDisplayManager.Instance;
        mainCamera = Camera.main;

        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        if (cfg.GetString("-worldCache", out transformCache))
        {
            string envDir;
            if (!cfg.GetString("-ABRConfig", out envDir))
                envDir = Environment.GetEnvironmentVariable("ABRConfig");

            if (envDir == null)
                envDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            cacheXform = true;
            worldFile = string.Format("{0}/{1}", envDir, transformCache);

            if (File.Exists(worldFile))
            {
                string[] transforms = System.IO.File.ReadAllLines(worldFile);

                if (transforms.Length > 0)
                {
                    string json = transforms[transforms.Length - 1];
                    WorldState worldState = JsonUtility.FromJson<WorldState>(json);

                    transform.position = worldState.P;
                    transform.rotation = worldState.Q;
                }
            }
        }
    }
    
    void saveState()
    {
        if (! cacheXform)
            return;
            
        WorldState worldState = new WorldState();

        worldState.Q = transform.rotation;
        worldState.P = transform.position;

        var json = JsonUtility.ToJson(worldState);

        try
        {
            if (File.Exists(worldFile))
            {
                var fn = File.AppendText(worldFile);
                fn.Write(json);
                fn.Write("\n");
                fn.Close();
            }
            else
            {
                var fn = File.CreateText(worldFile);
                fn.Write(json);
                fn.Write("\n");
                fn.Close();
            }            
        }            
        catch
        {
            Debug.Log("Cannot create or write world transform history file " + worldFile);
        }
    }
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        bool changed = false;

		if (tdm.IsMaster())
		{   
            bool b = Input.GetMouseButtonDown(button);
            if (mouseIsDown && !b)
            {
                mouseIsDown = false;
                saveState();
                return;
            }

            bool c = Input.GetKey(KeyCode.LeftControl);
            bool a = Input.GetKey(KeyCode.LeftAlt);
            bool s = Input.GetKey(KeyCode.LeftShift);
            if (b ||
                (modifier == 'n' && !c && !a && !s) ||
                (modifier == 'c' && c) ||
                (modifier == 'a' && a) ||
                (modifier == 's' && s))
			{
				lastPosition = Input.mousePosition;
				mouseIsDown = true;
			}
			else if (mouseIsDown)
			{
				Vector3 currentPosition = Input.mousePosition;
				Vector3 deltaPosition = currentPosition - lastPosition;

				float inputX = deltaPosition.x * worldRotationSensitivity;
				float inputY = deltaPosition.y * worldRotationSensitivity;

                if (inputX != 0 || inputY != 0)
                {
                    changed = true;
                    
                    Quaternion q_r, q_u;

                    if(Input.GetKey("space"))
                        q_r = Quaternion.AngleAxis(inputY, mainCamera.transform.forward);
                    else
                        q_r = Quaternion.AngleAxis(inputY, mainCamera.transform.right);

                    q_u = Quaternion.AngleAxis(-inputX, mainCamera.transform.up);

                    transform.rotation = q_r * q_u * transform.rotation;
                }

				lastPosition = currentPosition;
			}

			if (changed && tdm.NumberOfTiles() > 0)
            {
                WorldTransform wx = new WorldTransform();
                var fmt = new BinaryFormatter();
                var ms = new MemoryStream();
            
                Vector3 p;
                Quaternion r;
                transform.GetPositionAndRotation(out p, out r);

                wx.px = p.x;
                wx.py = p.y;
                wx.pz = p.z;
                wx.rx = r.x;
                wx.ry = r.y;
                wx.rz = r.z;
                wx.rw = r.w;

                fmt.Serialize(ms, wx);
                byte[] message = ms.ToArray();
                tdm.messageManager.SendMessage(message_tag, message);
            }	
		} 
		else
		{
            byte[] worldRotationMessaage = tdm.messageManager.GetMessage(message_tag);
            if (worldRotationMessaage != null)
            {
                var fmt = new BinaryFormatter();
                var ms = new MemoryStream(worldRotationMessaage);
                WorldTransform wx = (WorldTransform)fmt.Deserialize(ms);

                Vector3 p = new Vector3(wx.px, wx.py, wx.pz);
                Quaternion r = new Quaternion(wx.rx, wx.ry, wx.rz, wx.rw);

                transform.SetPositionAndRotation(p, r);
            }
		}
	}
}

