using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using IVLab.ABREngine;

public class ScreenShot : MonoBehaviour
{
    static int shotCount = 0;
    public string screenshot_template = "screenshot_{0}.png";
    static string screenshot_cache = "";
    void Start()
    {
        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();
        
        string s;
        if (cfg.GetString("-screenshotCache", out s))
            screenshot_cache = Path.Combine(ABREngine.Instance.Config.abr_root, s);
        else
            screenshot_cache = Path.Combine(ABREngine.Instance.Config.abr_root, "screenshots");

        if (! Directory.Exists(screenshot_cache))
        {
            Debug.Log("Screenshot directory " + screenshot_cache + " does not exist");
            screenshot_cache = "";
        }
        else
        {
                Debug.Log("Using ScreenShot Cache: " + screenshot_cache);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && screenshot_cache != "")
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
