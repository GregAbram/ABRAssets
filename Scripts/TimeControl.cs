using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IVLab.ABREngine;
using UnityEngine;

public class TimeControl : MonoBehaviour
{
    public int frame = 0;
    public int duration = 30;
    public bool running = false;
    int fps = 166;

    public bool isRunning()  { return running; }

    void Start()
    {
        ABREngine.Instance.SetScaleTime(0.0f);

        tLast = 0.0f;

        Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        if (cfg.GetString("-TimeControl-default", out string s))
            frame = int.Parse(s);

        if (cfg.GetString("-TimeControl-duration", out s))
            duration = int.Parse(s);

        if (cfg.GetString("-TimeControl-fps", out s))
            fps = int.Parse(s);

        ABREngine.Instance.SetScaleTime(0.0f);
    }

    float tLast = 0;

    // Update is called once per frame
    void Update()
    {
        float  totFrames = fps * duration;
        float stepSize = 1.0f / totFrames;

        //stepSize = 1.0f / 498.0f;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            tLast = tLast + stepSize;
            ABREngine.Instance.SetScaleTime(tLast);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            tLast = tLast - stepSize;
            ABREngine.Instance.SetScaleTime(tLast);
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            if (!running)
            {
                tLast = 0;
                ABREngine.Instance.SetScaleTime(0.0f);
                running = true;
            }
            else
                running = false;
            
        }

        if (running)
        {
            float tNext = tLast + stepSize;
            ABREngine.Instance.SetScaleTime(tNext);
            tLast = tNext;
            if (tNext >= 1.0)
            {
                tNext = 1.0f;
                running = false;
            }
        }
    }
}
