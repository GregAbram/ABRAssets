using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using IVLab.ABREngine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using IVLab.Utilities;

// The communication pattern is that the Server processes messages from above and the MessageRecipients represent 
// connections downward.   So the master has no Server and multiple recipients, while the clients have Servers
// but no recipients.

public class MessageRecipient
{
    TcpClient _client;
    NetworkStream _stream;

    public MessageRecipient(string host, int port)
    {
        _client = new(host, port);
        _stream = _client.GetStream();
    }   

    public void SendInt(int n)
    {
        byte[] bytes = System.BitConverter.GetBytes(n);
        _stream.Write(bytes, 0, bytes.Length);
    }

    public void SendBytes(byte[] bytes)
    {
        SendInt(bytes.Length);
        _stream.Write(bytes, 0, bytes.Length);
    }

    public void SendString(string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        SendBytes(bytes);
    }
}
public class TiledDisplayManager : MonoBehaviour
{
    public class MessageManager
    {
        private int _port;
        private List<MessageRecipient> _recipients = new List<MessageRecipient>();
        private Dictionary<string, byte[]> _messages = new Dictionary<string, byte[]>();
        public class MessageRecipient
        {
            NetworkStream _stream;

            public MessageRecipient(string host, int port)
            {
                TcpClient client = new(host, port);
                _stream = client.GetStream();
            }
            public void SendInt(int n)
            {
                byte[] bytes = System.BitConverter.GetBytes(n);
                _stream.Write(bytes, 0, bytes.Length);
            }

            public void SendBytes(byte[] bytes)
            {
                SendInt(bytes.Length);
                _stream.Write(bytes, 0, bytes.Length);
            }

            public void SendString(string str)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                SendBytes(bytes);
            }
        }
        public MessageManager(int p)
        {
            _port = p;
        }
        public void AddRecipient(string host)
        { 
            _recipients.Add(new MessageRecipient(host, _port));
        }

        public byte[] GetMessage(string tag)
        {
            byte[] msg = null;

            lock(_messages)
            {
                if (_messages.ContainsKey(tag))
                {
                    msg = _messages[tag];
                    _messages.Remove(tag);
                }
            }

            return msg;
        }
        public void SendMessage(string tag, byte[] msg)
        {
            foreach (var recipient in _recipients)
            {
                lock(recipient)
                {
                    recipient.SendString(tag);
                    recipient.SendBytes(msg);
                }
            }
        }

        private byte[] _ReadBytes(TcpClient sender)
        {
            byte[] lbuf = new byte[4];
            for (int m, k = 0; k < 4; k += m)
                m = sender.GetStream().Read(lbuf, k, 4-k);
                
            int len = BitConverter.ToInt32(lbuf, 0);

            byte[] bytes = new byte[len];
            for (int m, k = 0; k < len; k += m)
                m = sender.GetStream().Read(bytes, k, len-k);
            
            return bytes;
        }

        private void Handler(TcpClient client)
        {
            while (true)
            {
                byte[] tagBytes = _ReadBytes(client);
                string tag = System.Text.Encoding.UTF8.GetString(tagBytes);  

                if (tag == "quit")
                    Environment.Exit(0);
                
                byte[] msg = _ReadBytes(client);

                lock(_messages)
                {
                    _messages[tag] = msg;
                }
            }
        }

        public void StartServer()
        {
            TcpListener listener = new(IPAddress.Any, _port);    
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();
            Task.Run(() => Handler(client));
        }
    }
    [Serializable]
    public class Master
    {
        public string host;
        public string ip;
        public float aspect;
    };

    [Serializable]
    public class Dimensions
    {
        public int numTilesHeight;
        public int numTilesWidth;
        public int screenWidth;
        public int screenHeight;
        public int mullionHeight;
        public int mullionWidth;
        public bool fullScreen;
        public float aspect;
        public float scaling;

    };

    [Serializable]
    public class Process
    {
        public string host;
        public string ip;
        public int x;
        public int y;
        public int i;
        public int j;
    };

    [Serializable]
    public class  Wall
    {
        public Master master;  
        public Dimensions dimensions;
        public Process[] processes;
    };

    private bool initialized = false;
    public static TiledDisplayManager Instance { get; private set; }
    public bool isMaster = true;
    public MessageManager messageManager = null;

    int numProcesses = -1;
    int myRank = 0;
    float left, right, top, bottom, aspect;

    float wall_scaling = 1;

    int numberOfTiles = 0;  

    public int NumberOfTiles()
    {
        return Instance.numberOfTiles;
    }    
    
    public float WallScaling()
    {
        return Instance.wall_scaling;
    }

    public float GetWallScaling()
    {
        return Instance.wall_scaling;
    }
    public void GetOffset(out float l, out float r, out float b, out float t)
    {
        l = Instance.left;
        r = Instance.right; 
        b = Instance.bottom;
        t = Instance.top; 
    }
    public bool IsMaster()
    {
        return Instance.isMaster;
    }

    public TiledDisplayManager()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void OnEnable()
    {
        if (! Instance.initialized)
            Instance.Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        string wallConfigFileName;
        if (! cfg.GetString("wallConfig", out wallConfigFileName))
        {
            string home = System.Environment.GetEnvironmentVariable("USERPROFILE");
            if (home == null)
                home = System.Environment.GetEnvironmentVariable("HOME");
            Debug.Log("HOME is " + home);
            wallConfigFileName = Path.Combine(home, "wall.json");
        }

        if (! File.Exists(wallConfigFileName))
        {
            isMaster = true;
            return;
        }

        Wall container = new();
    
        try
        {
            StreamReader sr = new(wallConfigFileName);
            string json = sr.ReadToEnd();
            container = JsonUtility.FromJson<Wall>(json);
            sr.Close();
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
            Application.Quit();
        }

        List<string> hosts = new List<string>();
        var master = (container.master.ip == null) ? container.master.host : container.master.ip;
        hosts.Add(master);

        foreach (Process p in container.processes)
            hosts.Add((p.ip == null) ? p.host : p.ip);

        numProcesses = hosts.Count;
        numberOfTiles = hosts.Count - 1;

        string me = null;
        if (! cfg.GetString("-hostname", out me))
            me = SystemInfo.deviceName.Split('.')[0];

        isMaster = me == container.master.host;

        if (isMaster)
        {
            Debug.Log("Acting as master");
            var aspect_string = container.master.aspect;

            aspect = container.master.aspect;
            left = -aspect / 2;
            right = aspect / 2;
            bottom = -0.5F;
            top = 0.5F;
        }
        else
        {
            Debug.Log("Acting as worker");
        
            // Turn off GUI elements
            GameObject gui = GameObject.Find("GUI");
            if (gui != null)
                gui.SetActive(false);

            var aspect_string = container.dimensions.aspect;
            aspect = container.dimensions.aspect;
            wall_scaling = container.dimensions.scaling;

            var nth = container.dimensions.numTilesHeight;
            if (nth == 0) nth = 1;

            var ntw = Convert.ToDouble(container.dimensions.numTilesWidth);
            if (ntw ==  0) ntw = 1;

            var ch = nth / 2.0F;
            var cw = ntw / 2.0F;

            int knt = 1;
            bool found = false;
            foreach (Process p in container.processes)
            {
                string aa = p.host;

                if (me == aa)
                {
                    left = (float)((p.i - cw) * aspect);
                    right = left + aspect;
                    bottom = (float)((ch - 1) - (p.j) * 1.0F);
                    top = bottom + 1;
                    myRank = knt;
                    found = true;
                }
                knt = knt + 1;
            }
            if (!found)
            {
                Debug.LogFormat("Config file does not contain system named {0}", me);
                left = -aspect / 2;
                right = aspect / 2;
                bottom = -0.5F;
                top = 0.5F;
            }
        }

        messageManager = new MessageManager(1901);

        if (myRank > 0 && (numProcesses > 1))
        {
            Debug.Log("opening UP socket and waiting...");
            messageManager.StartServer();
            Debug.Log("UP connected");
        }
        else
        {
            for (int i = 1; i < numProcesses; i++)
            {
                messageManager.AddRecipient(hosts[i]);
                Debug.Log("connected to " + hosts[i]);
            }
        }
    }
    void Update()
    {
        if (Instance.IsMaster())
        {
            float time = ABREngine.Instance.GetCurrentTime();
            byte[] bytes = BitConverter.GetBytes(time);
            messageManager.SendMessage("CurrentTime", bytes);
        }
        else
        {
            byte[] bytes = messageManager.GetMessage("CurrentTime");
            if (bytes != null)
            {
                float time = BitConverter.ToSingle(bytes);
                ABREngine.Instance.SetCurrentTime(time);
            }
        }
    }
    
}
