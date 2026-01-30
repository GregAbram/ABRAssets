using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using IVLab.ABREngine;
using UnityEngine.InputSystem;


public class ScreenShot : MonoBehaviour
{
    static int shotCount = 0;
    public string screenshot_template = "screenshot_{0}.png";
    static string screenshot_cache = "";
    void Start()
    {
        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        if (!cfg.GetString("-screenshotCache", out screenshot_cache))
            screenshot_cache = "screenshots";

        string envDir;

        if (!cfg.GetString("-ABRConfig", out envDir))
            envDir = Environment.GetEnvironmentVariable("ABRConfig");

        if (envDir == null)
            envDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        screenshot_cache = string.Format("{0}/{1}", envDir, screenshot_cache);

        if(!Directory.Exists(screenshot_cache))
        {
            Directory.CreateDirectory(screenshot_cache);
        }    
    }

    // Update is called once per frame
    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool a = Keyboard.current.aKey.wasPressedThisFrame;
#else
        bool a = Input.GetKeyDown(KeyCode.A);
#endif 
        
        if (a && screenshot_cache != "")
        {
            int multiplier;

            string s;
            Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

            if (cfg.GetString("-screenshotMultiplier", out s))
                int.TryParse(s, out multiplier);
            else
                multiplier = 2;

            string filename = string.Format(screenshot_template, DateTime.Now.ToString("MM-dd-yyyy-h-mm-tt"));

            if (screenshot_cache != "")
                filename = screenshot_cache + "/" + filename;

            Debug.Log("Saving " + filename);
            ScreenCapture.CaptureScreenshot(filename, multiplier);
            shotCount = shotCount + 1;
        }
    }
}
