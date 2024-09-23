using System;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.EventSystems;



public class WorldRotation : MonoBehaviour
{
    public int button = 1;
    public float worldRotationSensitivity = 2f;
    Vector3 lastPosition;
    bool mouseIsDown = false;
    string worldFile = ""; 

    Camera mainCamera;

    TiledDisplayManager tdm = null;

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

        string transformCache;

        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        if (! cfg.GetString("-transformCache", out transformCache))
            transformCache = ".";

    	worldFile = string.Format("{0}/world", transformCache);

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

    void saveState()
    {
        WorldState worldState = new WorldState();

        worldState.Q = transform.rotation;
        worldState.P = transform.position;

        var json = JsonUtility.ToJson(worldState);

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
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

		if (tdm.IsMaster())
		{   
			if (Input.GetMouseButtonDown(button))
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

                Quaternion q_r, q_u;

                if(Input.GetKey("space"))
                    q_r = Quaternion.AngleAxis(inputY, mainCamera.transform.forward);
                else
                    q_r = Quaternion.AngleAxis(inputY, mainCamera.transform.right);

                q_u = Quaternion.AngleAxis(-inputX, mainCamera.transform.up);

                transform.rotation = q_r * q_u * transform.rotation;

				lastPosition = currentPosition;
			}

			if (Input.GetMouseButtonUp(button))
			{
				mouseIsDown = false;
				saveState();
			}

			if (tdm.NumberOfTiles() > 0)
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

                //Debug.Log("World sent " + wx.px + " " + wx.py + " " + wx.pz + " " + wx.rx + " " + wx.ry + " " + wx.rz + " " + wx.rw);

                fmt.Serialize(ms, wx);
                byte[] message = ms.ToArray();

                //Debug.Log("Sending Message " + message.Length + " " + message.ToString());

                tdm.Communicate(ref message);

            }	
		} 
		else
		{
            WorldTransform wx;
            var ms = new MemoryStream();
            var fmt = new BinaryFormatter();
            
			byte[] message = null;
			tdm.Communicate(ref message);

            ms = new MemoryStream(message);
            wx = (WorldTransform)fmt.Deserialize(ms);

            Vector3 p = new Vector3(wx.px, wx.py, wx.pz);
            Quaternion r = new Quaternion(wx.rx, wx.ry, wx.rz, wx.rw);

            transform.SetPositionAndRotation(p, r);
		}
	}
}

