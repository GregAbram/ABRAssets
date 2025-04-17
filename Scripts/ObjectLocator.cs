using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLocator : MonoBehaviour
{
    private ObjectStateManager objectState = null;
    static Vector3 lastPosition = Vector3.zero;
    static Quaternion lastRotation = Quaternion.identity;
    void Start()
    {
        // objectState = new ObjectStateManager();
        objectState = ScriptableObject.CreateInstance<ObjectStateManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.transform.position != lastPosition || gameObject.transform.rotation != lastRotation)
        {
            objectState.StashLocation(gameObject);
            lastPosition = gameObject.transform.position;
            lastRotation = gameObject.transform.rotation;
        }
    }
}
