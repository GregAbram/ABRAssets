
using UnityEngine;
using UnityEngine.InputSystem;


public class LookMove : CameraModel
{
    public override bool CameraController()
    {	
		bool save = false;

#if ENABLE_INPUT_SYSTEM
        bool b = false;
        switch (button)
        {
            case 0: b =  Mouse.current.leftButton.wasPressedThisFrame; break;
            case 1: b =  Mouse.current.rightButton.wasPressedThisFrame; break;
            case 2: b =  Mouse.current.middleButton.wasPressedThisFrame; break;
        }

        bool c = Keyboard.current.ctrlKey.isPressed;
        bool a = Keyboard.current.altKey.isPressed;
        bool s = Keyboard.current.shiftKey.isPressed;        

        if (b && ((modifier == 'n' && !c && !a && !s) || (modifier == 'c' && c) || (modifier == 'a' && a) || (modifier == 's' && s)))
        {
            mouseIsDown = true;
            lastPosition = Mouse.current.position.ReadValue();
        }
        
        if (!b && ((modifier == 'n' && !c && !a && !s) || (modifier == 'c' && c) || (modifier == 'a' && a) || (modifier == 's' && s)))
        { 
            switch (button)
            {
                case 0: b =  Mouse.current.leftButton.wasReleasedThisFrame; break;
                case 1: b =  Mouse.current.rightButton.wasReleasedThisFrame; break;
                case 2: b =  Mouse.current.middleButton.wasReleasedThisFrame; break;
            }

            if (b)
                mouseIsDown = false;
        }  
#else        
        bool b = Input.GetMouseButton(button);
        bool c = Input.GetKey(KeyCode.LeftControl);
        bool a = Input.GetKey(KeyCode.LeftAlt);
        bool s = Input.GetKey(KeyCode.LeftShift);        
        
        if (!b && mouseIsDown)
        {
            mouseIsDown = false;
            return true;
        }
        else if (b && ((modifier == 'n' && !c && !a && !s) || (modifier == 'c' && c) || (modifier == 'a' && a) || (modifier == 's' && s)))
        {
            if (! mouseIsDown)
            {
                lastPosition =  Input.mousePosition;
                mouseIsDown = true;
            }
        }
#endif

		if (mouseIsDown)
		{
#if ENABLE_INPUT_SYSTEM
            Vector2 currentPosition = Mouse.current.position.ReadValue();
            Vector2 deltaPosition = currentPosition - lastPosition;
            lastPosition = currentPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;
#else
            Vector3 currentPosition = Input.mousePosition;
            Vector3 deltaPosition = currentPosition - lastPosition;
            lastPosition = currentPosition;

            float inputX = deltaPosition.x * mouseRotationSensitivity;
            float inputY = deltaPosition.y * mouseRotationSensitivity;
#endif

			if (inputX != 0 || inputY != 0)
			{
				Quaternion q_r, q_u;
				q_r = Quaternion.AngleAxis(inputY, Camera.main.transform.right);
				q_u = Quaternion.AngleAxis(-inputX, Camera.main.transform.up);
				transform.rotation = q_r * q_u * transform.rotation;
				transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
				save = true;
			}
		}

#if ENABLE_INPUT_SYSTEM
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        float inputSW = scroll.y;
#else
        float inputSW = Input.GetAxis("Mouse ScrollWheel") * mouseMovementSensitivity;
#endif
		if (inputSW != 0)
		{
#if ENABLE_INPUT_SYSTEM
			bool space = Keyboard.current.spaceKey.wasPressedThisFrame;
#else
			bool space = Input.GetKey("space");
#endif
			if (space)
				transform.position += inputSW * transform.up;
			else
				transform.position += inputSW * transform.forward;
		
			save = true;

		}				

		return save;
    }
}

