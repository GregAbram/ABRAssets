using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.IO;
using System;
using Unity.VisualScripting;


public class ObjectStateManager : ScriptableObject
{
    public static ObjectStateManager Instance { get; set; } = null;
    Dictionary<string, Location> objectLocations;

    public string stateFile = "/Users/gda/objectState.json";

    private void OnDestroy()
    {
        SaveObjectLocations(stateFile);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }

        Instance = this;
        Instance.objectLocations = new Dictionary<string, Location>();

        if (File.Exists(stateFile))
        {
            LoadObjectLocations(stateFile);
        }
    }

    [System.Serializable]
    struct Location {
        public float px, py, pz;
        public float qx, qy, qz, qw;

        public Location(Vector3 p, Quaternion q)
        {
            px = p.x;
            py = p.y;
            pz = p.z;
            qx = q.x;
            qy = q.y;
            qz = q.z;
            qw = q.w;
        }

        public void SetObjectLocation(ref GameObject g)
        {
            g.transform.position = new Vector3(px, py, pz);
            g.transform.rotation = new Quaternion(qx, qy, qz, qw);
        }
    }


    public void StashLocation(GameObject go)
    {
        if (Instance.objectLocations.ContainsKey(go.name))   
            Instance.objectLocations[go.name] = new Location(go.transform.position, go.transform.rotation);
        else
            Instance.objectLocations.Add(go.name, new Location(go.transform.position, go.transform.rotation));
    }

    public void SaveObjectLocations(string filename)
    {
        string json = JsonConvert.SerializeObject(Instance.objectLocations);
        File.WriteAllText(filename, json);
    }

    public void LoadObjectLocations(string filename)
    {
        string json = File.ReadAllText(filename);
        objectLocations = JsonConvert.DeserializeObject<Dictionary<string,Location>>(json);
        foreach (KeyValuePair<string,Location> pair in Instance.objectLocations)
        {
            GameObject go = GameObject.Find(pair.Key);
            if (go)
            {
                Location location = pair.Value;
                location.SetObjectLocation(ref go);
            }
            else
            {
                Console.WriteLine("Located object{0} not found", pair.Key);
            }
        }
    }
}