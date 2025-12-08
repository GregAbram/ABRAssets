using System;
using System.Collections.Generic;
using System.Data.Common;
using IVLab.ABREngine;
using Unity.VisualScripting;
using UnityEngine;

public class ABRPicker : MonoBehaviour
{
    public List<GameObject> listeners;

    public struct ABRPick
    {
        public Vector3 point;
        public Vector3 barycentric_weights; 
        public GameObject abrGameObject;
        public Guid guid;
        public int instanceId;
    };

    public void Raycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ABRPick abrPick = new ABRPick();
            abrPick.point = hit.point;
         
            GameObject abrGO = hit.collider.gameObject;

            if (abrGO.TryGetComponent<IVLab.ABREngine.InstanceId>(out IVLab.ABREngine.InstanceId pickId))
            {
                abrPick.instanceId = pickId.id;
            }
            else
            {
                abrPick.instanceId = -1;
            }

            if (abrGO.name.Contains("ABR Surface"))
            {
                abrPick.abrGameObject = abrGO;
                Debug.Log("its a surface " + abrPick.abrGameObject.name);
            }
            else if (abrGO.name.Contains("ABR Glyph"))
            {
                abrPick.abrGameObject = abrGO.transform.parent.parent.gameObject;
                Debug.Log("Its a glyph " + abrPick.abrGameObject.name);      
            }      
            else if (abrGO.name.Contains("ABR Line"))
            {
                abrPick.abrGameObject = abrGO.transform.parent.parent.gameObject;
                Debug.Log("Its a line in " + abrPick.abrGameObject.name);
            }
            else 
            {
                Debug.Log("Its not an ABR object");
                return;
            }

            if (! abrPick.abrGameObject.TryGetComponent<EncodedGameObject>(out EncodedGameObject ego))
            {
                Debug.Log("picked an ABR objhect that does not have a EGO");
                return;
            }

            abrPick.guid = ego.Uuid;

            foreach (GameObject listener in listeners)
            {
                PickHandler handler = listener.GetComponent<PickHandler>();
                if (handler != null) handler.onPick(abrPick);
            }
        }        
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // right mouse button
        {
            Raycast();
        }
    }
}
