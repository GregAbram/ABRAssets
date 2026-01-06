using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Startup : MonoBehaviour
{
    void Start()
    {
        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        string scene;
        if (cfg.GetString("-startScene", out scene))
            SceneManager.LoadScene (scene);
        else
            Debug.Log("no startup scene given");
    }


}
