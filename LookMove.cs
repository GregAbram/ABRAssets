using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;

public class LookMove : CameraModel
{
    public override void CameraController()
    {	
		if(Input.GetKeyDown("r"))
		{
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}

	    if (Input.GetMouseButtonDown(button))
		{
	        mouseIsDown = true;
			lastPosition = Input.mousePosition;
			moved = false;
		}
		else if (Input.GetMouseButtonUp(button))   
		{
			mouseIsDown = false;
			saveState = moved;
		}
	   
	    if (mouseIsDown)
	    {
			moved = true;

            Vector3 currentPosition = Input.mousePosition;
            Vector3 deltaPosition = currentPosition - lastPosition;
			lastPosition = currentPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;

            Quaternion q_r, q_u;
            q_r = Quaternion.AngleAxis(inputY, Camera.main.transform.right);
            q_u = Quaternion.AngleAxis(-inputX, Camera.main.transform.up);

            transform.rotation = q_r * q_u * transform.rotation;
		}

		float inputSW = Input.GetAxis("Mouse ScrollWheel") * mouseMovementSensitivity;
		if (inputSW != 0)
		{
			if (Input.GetKey("space"))
				transform.position += inputSW * transform.up;
			else
				transform.position += inputSW * transform.forward;
		
			moved = true;
		}	
     }
}

