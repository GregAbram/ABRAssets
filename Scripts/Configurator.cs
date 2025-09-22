using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq; 
using Unity.VisualScripting;
using UnityEngine.Rendering;
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
            return;
        }

        Instance = this;

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
                cfg[args[i]] = args[i + 1];
            }
        }

        string envDir;
        if (cfg.ContainsKey("-ABRConfig"))
            envDir = cfg["-ABRConfig"];
        else
        {
            envDir = Environment.GetEnvironmentVariable("ABRConfig");
            if (envDir != null)
                envDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        try
        {
            string cfgFile = Path.Combine(envDir, "svis.json");

            using (StreamReader sr = new(cfgFile))
            {
                string jsonString = sr.ReadToEnd();
                var jsonObject = JObject.Parse(jsonString);
                Dictionary<string, string> dictObj = jsonObject.ToObject<Dictionary<string, string>>();
                foreach (var key in dictObj.Keys)
                {
                    cfg[key] = dictObj[key];
                }
            }
        }
        catch
        {
            Debug.Log("No configuration file");
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

