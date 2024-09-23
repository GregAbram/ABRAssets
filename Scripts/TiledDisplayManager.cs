using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEditor;
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

        IPHostEntry ipHE = Dns.GetHostEntry(srvr);
        if (ipHE.AddressList.Length == 0)
        {
            Debug.LogFormat("Error getting IP address for {0}", srvr);
            Application.Quit();
        }

        IPAddress ipAddress = ipHE.AddressList[0];
        var ipEP = new IPEndPoint(ipAddress, 1902);
        skt = new Socket(ipEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            skt.Connect(ipEP);
        }
        catch
        {
            Debug.LogFormat("unable to connnect to {0}", srvr);
            skt = null;
            Application.Quit();
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

    public string configFile = "./configuration.xml";
    public bool isMaster = true;

    MySocket up = null;

    List<MySocket> child_sockets = new List<MySocket>();

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
                if (skt != null && skt.IsConnected())
                {
                    skt.Send(szBytes);
                    skt.Send(serialized_message);                    
                }

            }

            foreach (MySocket skt in child_sockets)
                if (skt != null && skt.IsConnected())
                    skt.Receive(ref szBytes);
        }
        else
        {
            if (up == null || !up.IsConnected())
                throw(new Exception("parent has disappeared"));
    
            up.Receive(ref szBytes);
            sz = BitConverter.ToInt32(szBytes);
            serialized_message = new byte[sz];
            up.Receive(ref serialized_message);
            up.Send(szBytes);
        }
    }
    
}
