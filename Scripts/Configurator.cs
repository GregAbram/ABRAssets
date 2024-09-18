using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public class Configurator : ScriptableObject
{
    public static Configurator Instance { get; set; }

    private Dictionary<string, string> cfg = new Dictionary<string, string>();

    private void Awake()
    {   
        if (Instance != null && Instance != this)
        {
            //Destroy(this);
            return;
        }

        Instance = this;

        string envDir = Environment.GetEnvironmentVariable("ABRConfig");
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (envDir == null)
            envDir = home;
     
        string cfgFile = Path.Combine(envDir, "svis.json");

        try
        {
            string s = File.ReadAllText(cfgFile);
            JObject json = JObject.Parse(s);
            foreach (var i in json)
            {
                cfg[i.Key] = i.Value.ToString();
            }
        }
        catch
        {
            Debug.Log("No configuration file");
        }

        var args = System.Environment.GetCommandLineArgs();

        for (var i = 1; i < args.Length; i += 2)
        {
            var a = args[i];
            if (a[0] != '-' | i == (args.Length - 1))
            {
                Debug.Log("command line error");
                break;
            }
            else 
            {
                cfg[args[i]] = args[i+1];
            }
        }

    }

    public bool GetString(string k, out string v) 
    { 
        if (Instance.cfg.ContainsKey(k))
        {
            v = Instance.cfg[k];
            return true;
        }
        else
        {
            v = "none";           
            return false;
        }
    }

    public List<string> keys() { return Instance.cfg.Keys.ToList(); }
} 

