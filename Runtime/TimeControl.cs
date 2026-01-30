
using IVLab.ABREngine;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeControl : MonoBehaviour
{
    public int frame = 0;
    public int duration = 30;
    public bool running = false;
    public int fps = 166;

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

#if ENABLE_INPUT_SYSTEM
        bool u = Keyboard.current.upArrowKey.wasPressedThisFrame;
        bool d = Keyboard.current.downArrowKey.wasPressedThisFrame;
        bool p = Keyboard.current.pKey.wasPressedThisFrame;

#else
        bool u = Input.GetKeyDown(KeyCode.UpArrow);
        bool d = Input.GetKeyDown(KeyCode.DownArrow);
        bool p = Input.GetKeyDown(KeyCode.P);
#endif 
        if (u)
        {
            tLast = tLast + stepSize;
            ABREngine.Instance.SetScaleTime(tLast);
        }
        else if (d)
        {
            tLast = tLast - stepSize;
            ABREngine.Instance.SetScaleTime(tLast);
        }
        else if (p)
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
