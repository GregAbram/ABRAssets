using System.Collections;
using System.Collections.Generic;
using IVLab.ABREngine;
using UnityEngine;

public class PickTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // right mouse button
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 hitPoint = hit.point;
                Debug.Log($"Hit Point: {hitPoint} on object {hit.collider.gameObject.name}  ");
            }
            else
            {
                Debug.Log("No hit detected.");
            }
        }
    }
}
