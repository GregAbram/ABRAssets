using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering;

public class MySocket 
{
    Socket skt = null;

    public MySocket()
    {
        Socket srvr = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        srvr.Bind(new IPEndPoint(IPAddress.Any, 1902));
        srvr.Listen(1);
        Debug.Log("Listening...............");
        skt = srvr.Accept();
        Debug.Log("Accepted!");
    }

    public MySocket(string srvr)
    {   
        Debug.LogFormat("Trying to connect to tile server on {0}", srvr);

        IPHostEntry he = Dns.GetHostEntry(srvr);
        if (he.AddressList.Length == 0)
        {
            Debug.LogFormat("Error getting IP address for {0}", srvr);
            Application.Quit();
        }

        skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        foreach (var addr in he.AddressList)
        {
            var conn = new IPEndPoint(addr, 1902);
            try
            {
                skt.Connect(conn);
                break;
            }
            catch
            {
            }
        }
    }

    public bool IsConnected()
    {
        return skt.Connected;   
    }

    public void Send(byte[] bytes)
    {
        int remaining = bytes.Length;
        int offset = 0, r;
        do
        {
            r = skt.Send(bytes, offset, remaining, SocketFlags.None);
            remaining -= r; 
            offset += r;    
        } while (remaining > 0 && r != 0); 

        if (r == 0)
            Application.Quit();       
    }

    public void Receive(ref byte[] bytes)
    {

        int remaining = bytes.Length;
        int offset = 0, r;
        do
        {
            r = skt.Receive(bytes, offset, remaining, SocketFlags.None);
            remaining -= r;
            offset += r;
        } while (remaining > 0 && r != 0); 

        if (r == 0)
            Application.Quit();
    }
}

public class TiledDisplayManager : ScriptableObject
{
    private bool initialized = false;
    
    public static TiledDisplayManager Instance { get; private set; }

    public string configFile = "./configuration.xml";
    public bool isMaster = true;

    MySocket up = null;

    List<MySocket> child_sockets = new List<MySocket>();

    int numProcesses = -1;
    int myRank = 0;
    float left, right, top, bottom, aspect;

    float wall_scaling = 1;

    [Serializable]
    public class dimensions
    {
        [XmlAttribute] public string numTilesWidth;
        [XmlAttribute] public string numTilesHeight;
        [XmlAttribute] public string screenWidth;
        [XmlAttribute] public string screenHeight;
        [XmlAttribute] public string mullionWidth;
        [XmlAttribute] public string mullionHeight;
        [XmlAttribute] public string fullscreen;
        [XmlAttribute] public string aspect;

        [XmlAttribute] public float wall_scaling;
    }

    int numberOfTiles = 0;  

    public int NumberOfTiles()
    {
        return Instance.numberOfTiles;
    }    
    
    public float WallScaling()
    {
        return Instance.wall_scaling;
    }

    [Serializable]
    public class process
    {
        [XmlAttribute] public string host;
        [XmlAttribute] public string ip;
        [XmlAttribute] public string display;
        [XmlAttribute] public string port;
        [XmlAttribute] public string x;
        [XmlAttribute] public string y;
        [XmlAttribute] public string i;
        [XmlAttribute] public string j;
    }

    [Serializable]
    public class master
    {
        [XmlAttribute] public string host;
        [XmlAttribute] public string aspect;
        [XmlAttribute] public string display;
        [XmlAttribute] public string ip;
    }

    [XmlRoot("tile_configuration")]
    public class tile_configuration
    {
        [XmlElement] public dimensions dimensions;
        [XmlElement] public master master;
        [XmlArray] public process[] processes;
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
            wallConfigFileName = Path.Combine(home, "wall.xml");
        }

        if (! File.Exists(wallConfigFileName))
        {
            isMaster = true;
            return;
        }

        tile_configuration container = null;
    
        try
        {
            var serializer = new XmlSerializer(typeof(tile_configuration));
            var stream = new FileStream(wallConfigFileName, FileMode.Open);
            container = serializer.Deserialize(stream) as tile_configuration;
            stream.Close();   
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
            Application.Quit();
        }


        List<string> hosts = new List<string>();
        var master = (container.master.ip == null) ? container.master.host : container.master.ip;
        hosts.Add(master);


        foreach (process p in container.processes)
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

            var parts = aspect_string.Split(':');
            aspect = (float)(Convert.ToDouble(parts[0]) / Convert.ToDouble(parts[1]));

            left = -aspect / 2;
            right = aspect / 2;
            bottom = -0.5F;
            top = 0.5F;
        }
        else
        {
            Debug.Log("Acting as worker");
            var aspect_string = container.dimensions.aspect;
            var parts = aspect_string.Split(':');
            aspect = (float)(Convert.ToDouble(parts[0]) / Convert.ToDouble(parts[1]));

            wall_scaling = container.dimensions.wall_scaling;

            var ch = Convert.ToDouble(container.dimensions.numTilesHeight) / 2.0F;
            var cw = Convert.ToDouble(container.dimensions.numTilesWidth) / 2.0F;

            int knt = 1;
            bool found = false;
            foreach (process p in container.processes)
            {
                string aa = p.host;

                if (me == aa)
                {
                    left = (float)((Convert.ToDouble(p.i) - cw) * aspect);
                    right = left + aspect;
                    bottom = (float)((ch - 1) - (Convert.ToDouble(p.j)) * 1.0F);
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

        if (myRank > 0 && (numProcesses > 1))
        {
            Debug.Log("opening UP socket and waiting...");
            up = new MySocket();
            Debug.Log("UP connected");
        }
        else
        {
            for (int i = 1; i < numProcesses; i++)
            {
                MySocket skt = new MySocket(hosts[i]);
                child_sockets.Add(skt);
            }
        }
    }

    public void Communicate(ref byte[] serialized_message)
    {
        Instance._Communicate(ref serialized_message);
    }

    private void _Communicate(ref byte[] serialized_message)
    {
        byte[] szBytes = new byte[4];
        int sz = 0;

        if (isMaster)
        {
            szBytes = BitConverter.GetBytes((Int32)serialized_message.Length);
            foreach (MySocket skt in child_sockets)
            {
                skt.Send(szBytes);
                skt.Send(serialized_message);
            }

            foreach (MySocket skt in child_sockets)
                skt.Receive(ref szBytes);
        }
        else
        {
            up.Receive(ref szBytes);
            sz = BitConverter.ToInt32(szBytes);
            serialized_message = new byte[sz];
            up.Receive(ref serialized_message);
            up.Send(szBytes);
        }
    }
    
}
