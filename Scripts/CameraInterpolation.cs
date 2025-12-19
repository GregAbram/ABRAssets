using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using IVLab.ABREngine;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class CameraInterpolation : MonoBehaviour
{
    class CState
    {
        public Vector3 P;
        public Quaternion R;
        public int n;
    }

    List<CState> keyFrames = new List<CState>();
    string envDir;

    bool saving = false;

    int frameNo = 0;

    int width, height;

    Camera cam;

    float totalTime = 0;

    string fileName;

    Configurator cfg = null;

    void LoadKeyFrames()
    {
        keyFrames.Clear();
        
        if (!File.Exists(fileName))
            UnityEngine.Debug.Log("No keyframes file\n");
        else
        {
            string[] strings = System.IO.File.ReadAllLines(fileName);
            for (int i = 0; i < strings.Length; i++)
            {
                var cState = JsonUtility.FromJson<CState>(strings[i]);
                totalTime = totalTime + cState.n;
                keyFrames.Add(cState);
            }
        }
    }

    void Start()
    {

        cfg = ScriptableObject.CreateInstance<Configurator>();

        if (!cfg.GetString("-ABRConfig", out envDir))
            envDir = Environment.GetEnvironmentVariable("ABRConfig");

        if (envDir == null)
            envDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        fileName = string.Format("{0}/KeyFrames.txt", envDir);

        LoadKeyFrames();

        cam = GetComponent<Camera>();
        width = Screen.width;
        height = Screen.height;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (saving)
                saving = false;
            else
            {
                saving = true;
                frameNo = 0;
            }
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            CState cState = new CState();

            cState.P = transform.position;
            cState.R = transform.rotation;
            if (keyFrames.Count == 0)
                cState.n = 0;
            else
                cState.n = 50;

            totalTime = totalTime + cState.n;
            keyFrames.Add(cState);

        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            using (StreamWriter sw = new StreamWriter(fileName, append: false))
            {
                foreach (var cs in keyFrames)
                {
                    var json = JsonUtility.ToJson(cs);
                    sw.Write(json);
                    sw.Write("\n");
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            LoadKeyFrames();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            keyFrames.Clear();
            totalTime = 0;
        }


        TimeControl timeControl = gameObject.GetComponent<TimeControl>();
        if (timeControl)
        {
            if (keyFrames.Count > 1)
            {
                float scaleTime = ABREngine.Instance.GetScaleTime();
                if (scaleTime > 1.0f)
                    return;

                float animationTime = 0;
                int interval;

                for (interval = 1; interval < keyFrames.Count; interval++)
                    animationTime = animationTime + keyFrames[interval].n;

                float targetTime = scaleTime * animationTime, t0 = 0;
                for (interval = 1; interval < keyFrames.Count; interval++)
                {
                    if (t0 + keyFrames[interval].n >= targetTime)
                        break;
                    t0 = t0 + keyFrames[interval].n;
                }

                float dt = 0;
                try
                {
                    dt = (targetTime - t0) / keyFrames[interval].n;
                }
                catch
                {
                    UnityEngine.Debug.Log("interval error");
                }

                Vector3 pLast = keyFrames[interval - 1].P;
                Quaternion qLast = keyFrames[interval - 1].R;

                Vector3 pNext = keyFrames[interval].P;
                Quaternion qNext = keyFrames[interval].R;

                transform.position = Vector3.Lerp(pLast, pNext, dt);
                transform.rotation = Quaternion.Lerp(qLast, qNext, dt);
            }
        }

        if (saving)
        {
#if false
            SaveFrame(frameNo);
#else
            StartCoroutine(SaveFrame(frameNo));
#endif
            frameNo = frameNo + 1;
        }
    }

    IEnumerator SaveFrame1(int fno)
    {
        yield return new WaitForEndOfFrame();

        string fileName = string.Format("{0}/frame_{1}.png", envDir, fno.ToString("D4"));
        if (System.IO.File.Exists(fileName))
            System.IO.File.Delete(fileName);

        Texture2D screenImage = new Texture2D(Screen.width, Screen.height);
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();
        byte[] imageBytes = screenImage.EncodeToPNG();
        System.IO.File.WriteAllBytes(fileName, imageBytes);
    }

    IEnumerator SaveFrame(int fno)
    {
        yield return new WaitForEndOfFrame();

        string fileName = string.Format("{0}/frame_{1}.png", envDir, fno.ToString("D4"));
        if (System.IO.File.Exists(fileName))
            System.IO.File.Delete(fileName);

       cfg.Log(string.Format("Saving {0}\n", fileName));

        ScreenCapture.CaptureScreenshot(fileName, 2);

#if false
        while (!System.IO.File.Exists(fileName))
        {
           Thread.Sleep(20);
        }
#else
        Thread.Sleep(1000);
#endif
        if (System.IO.File.Exists(fileName))
        {
            cfg.Log(string.Format("Saved {0}\n", fileName));
            frameNo = frameNo + 1;
        }
    }

}
