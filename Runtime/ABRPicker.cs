using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

using IVLab.ABREngine;
using UnityEditor;

public class ABRPicker : MonoBehaviour
{
    public List<GameObject> listeners;
     public XRRayInteractor rayInteractor;

    public int button = 1;
    public char modifier = 'n'; // n = none, c = ctrl, a = alt, s = shift

    public struct ABRPick
    {
        public Vector3 point;
        public Vector3 barycentric_weights; 
        public GameObject abrGameObject;
        public Guid guid;
        public int id;
        public RawDataset dataset;
    };

    public class ABRPickHandler : MonoBehaviour
    {
        public virtual  void onPick(ABRPick pick) { Debug.Log("baseclass pick"); }
    }

    public void ProcessHit(RaycastHit hit)
    {
        ABRPick abrPick = new ABRPick();
        abrPick.point = hit.point;        
        
        GameObject abrGO = hit.collider.gameObject;

        if (! abrGO.TryGetComponent<IVLab.ABREngine.InstanceId>(out IVLab.ABREngine.InstanceId pickId))
        {
            return;
        }

        if (abrGO.name.Contains("ABR Surface"))
        {
            abrPick.abrGameObject = abrGO;
            abrPick.id = hit.triangleIndex;
            abrPick.barycentric_weights = hit.barycentricCoordinate;
            
        }
        else if (abrGO.name.Contains("ABR Glyph"))
        {
            abrPick.id = pickId.id;
            abrPick.abrGameObject = abrGO.transform.parent.parent.gameObject;
        }      
        else if (abrGO.name.Contains("ABR Line"))
        {
                abrPick.id = pickId.id;
            abrPick.abrGameObject = abrGO.transform.parent.parent.gameObject;
        }
        else 
        {
            Debug.Log("Its not an ABR object");
            return;
        }

        if (! abrPick.abrGameObject.TryGetComponent<EncodedGameObject>(out EncodedGameObject ego))
        {
            Debug.Log("picked an ABR object that does not have a EGO");
            return;
        }

        abrPick.guid = ego.Uuid;
        
        IDataImpression idi = ABREngine.Instance.GetDataImpression(ego.Uuid);
        if (idi == null)
        {
            Debug.Log("No data impression found for pick");
            return;
        }

        if (ABREngine.Instance.Data.TryGetRawDataset(idi.GetKeyData()?.Path, out abrPick.dataset))
        {
            Debug.Log("Picked dataset: " + abrPick.dataset.dataPath);
        }
        else
        {
            Debug.Log("Picked dataset not found");
        }   

        ABRPickHandler handler = gameObject.GetComponent<ABRPicker.ABRPickHandler>();
        if (handler)
            handler.onPick(abrPick);

        if (listeners.Count > 0)
            foreach (GameObject listener in listeners)
            {
                handler = listener.GetComponent<ABRPicker.ABRPickHandler>();
                if (handler != null) 
                    handler.onPick(abrPick);
            }
    }        

    void Update()
    {
        if (rayInteractor)
        {
            if (rayInteractor.isSelectActive)
            {
                if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    ProcessHit(hit);
                }
            }
        }
        else
        {
            bool b = false, c, a, s;

#if ENABLE_INPUT_SYSTEM    
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            switch (button)
            {
                case 0: b =  Mouse.current.leftButton.wasPressedThisFrame; break;
                case 1: b =  Mouse.current.rightButton.wasPressedThisFrame; break;
                case 2: b =  Mouse.current.middleButton.wasPressedThisFrame; break;
            }

            c = Keyboard.current.ctrlKey.isPressed;
            a = Keyboard.current.altKey.isPressed;
            s = Keyboard.current.shiftKey.isPressed;    
#else
            Vector3 mousePosition = Input.mousePosition;
            b = Input.GetMouseButtonDown(button);       
            c = Input.GetKey(KeyCode.LeftControl);
            a = Input.GetKey(KeyCode.LeftAlt);
            s = Input.GetKey(KeyCode.LeftShift);        
#endif

       
        if (b && ((modifier == 'n' && !c && !a && !s) ||
                  (modifier == 'c' &&  c && !a && !s) ||
                  (modifier == 'a' && !c &&  a && !s) ||
                  (modifier == 's' && !c && !a &&  s)))
            { 
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    ProcessHit(hit);                    
                }
            }
        }
    }
}

