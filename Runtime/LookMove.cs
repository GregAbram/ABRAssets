using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

public class LookMove : CameraModel
{
    public override bool CameraController()
    {	
		bool save = false;

		if(Input.GetKeyDown("r"))
		{
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
			return true;
		}

        bool b = Input.GetMouseButton(button);
        if (mouseIsDown && !b)
        {
			mouseIsDown = false;
            return true;
        }

        bool c = Input.GetKey(KeyCode.LeftControl);
        bool a = Input.GetKey(KeyCode.LeftAlt);
        bool s = Input.GetKey(KeyCode.LeftShift);
        if (b &&
            (modifier == 'n' && !c && !a && !s) ||
            (modifier == 'c' && c) ||
            (modifier == 'a' && a) ||
            (modifier == 's' && s))
		{
			if (! mouseIsDown)
			{
				mouseIsDown = true;
				lastPosition = Input.mousePosition;
				return false;				
			}
			else
			{
				Vector3 currentPosition = Input.mousePosition;
				Vector3 deltaPosition = currentPosition - lastPosition;
				lastPosition = currentPosition;

				float inputX = deltaPosition.x * mouseRotationSensitivity;
				float inputY = deltaPosition.y * mouseRotationSensitivity;
				if (inputX != 0 || inputY != 0)
				{
					Quaternion q_r, q_u;
					q_r = Quaternion.AngleAxis(inputY, Camera.main.transform.right);
					q_u = Quaternion.AngleAxis(-inputX, Camera.main.transform.up);
					transform.rotation = q_r * q_u * transform.rotation;
					transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
					save = true;
				}
				
				float inputSW = Input.GetAxis("Mouse ScrollWheel") * mouseMovementSensitivity;
				if (inputSW != 0)
				{
					if (Input.GetKey("space"))
						transform.position += inputSW * transform.up;
					else
						transform.position += inputSW * transform.forward;
				
					save = true;

				}				
			}
		}

		return save;
     }
}

