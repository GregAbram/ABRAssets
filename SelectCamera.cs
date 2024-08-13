using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCamera : MonoBehaviour
{
    void Awake()
    {
        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        string camera;
        if (cfg.GetString("-camera", out camera))
        {
            foreach (Transform child in transform)
            {
                string s = child.name;
                Debug.Log(s);
                
                if (camera == child.name)
                {
                    child.gameObject.SetActive(true);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

        }
    }

}
