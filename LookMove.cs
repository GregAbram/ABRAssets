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
    public float forward_sensitivity = 1f;    
    public float mouse_sensitivity = 1f;
	float last_x = -1;
	float last_y = -1;
	private float head_rotation_X = 0f;
	private float head_rotation_Y = 0f;
	public float upDownLimit = 30f;
	private bool motion_mode = true;

	
	public override void Start() 
	{
		base.Start();

		head_rotation_X = transform.localEulerAngles.x;
		head_rotation_Y = transform.localEulerAngles.y;

		Configurator cfg = ScriptableObject.CreateInstance<Configurator>();

        string str;
        if (cfg.GetString("-lookMove-forward-sensitivity", out str))
		{
			float.TryParse(str, out forward_sensitivity);
		}

		if (cfg.GetString("-lookMove-mouse-sensitivity", out str))
		{
			float.TryParse(str, out mouse_sensitivity);
		}

		if (cfg.GetString("-lookMove-upDownLimit", out str))
		{
			float.TryParse(str, out upDownLimit);
		}

	}

    public override void CameraController()
    {	
		if (Input.GetKeyDown("h"))
		{
			motion_mode = false;
		}
		else if (Input.GetKeyUp("h"))
		{
			motion_mode = true;
		}


		if(Input.GetKeyDown("r"))
		{
			transform.position = Vector3.zero;
			transform.localEulerAngles = Vector3.zero;
			head_rotation_X = 0f;
			head_rotation_Y = 0f;
		}

	    if (Input.GetMouseButtonDown(button))
		{
	        mouseIsDown = true;
			last_x = Input.mousePosition.x;
		}
		else if (Input.GetMouseButtonUp(button))   
			mouseIsDown = false;
	   
		float forward = Input.GetAxis("Mouse ScrollWheel");

	    if (mouseIsDown)
	    {
			float leftRight = Input.GetAxis("Mouse X");			
			float upDown = Input.GetAxis("Mouse Y");
			float dx, dy;

			float x = Input.mousePosition.x;
			if (last_x != -1)
				dx = x - last_x;
			else	
				dx = 0;
			last_x = x;

			head_rotation_X +=  dx * mouse_sensitivity; 

			float y = Input.mousePosition.y;
			if (last_y != -1)
				dy = y - last_y;
			else
				dy = 0;
			last_y = y;

			head_rotation_Y +=  dy * mouse_sensitivity; 
			if (head_rotation_Y > upDownLimit) head_rotation_Y = upDownLimit;
			else if (head_rotation_Y < -upDownLimit) head_rotation_Y = -upDownLimit;

			transform.localEulerAngles = Vector3.up * head_rotation_X + Vector3.right * head_rotation_Y; 			
		}
		
		if (Input.GetMouseButtonUp(button))
		{
			mouseIsDown = false; 
			last_x = -1;
			last_y = -1;
		}

		if (motion_mode)
			transform.position += transform.forward * forward * forward_sensitivity * Time.deltaTime;  
		else
			transform.position += Vector3.up * forward * forward_sensitivity * Time.deltaTime;  
    }
}

