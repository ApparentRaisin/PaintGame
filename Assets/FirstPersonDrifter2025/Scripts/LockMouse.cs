// by @torahhorse

using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class LockMouse : MonoBehaviour
{
    
	void Start()
	{
		LockCursor(true);
	}

    bool lockState = true;
    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
    	// lock when mouse is clicked
    	if( mouse.leftButton.wasPressedThisFrame && Time.timeScale > 0.0f )
    	{
    		LockCursor(true);
    	}
    
    	// unlock when escape is hit
        if  ( keyboard.escapeKey.wasPressedThisFrame)
        {
        	LockCursor(!lockState);
        }
    }
    
    public void LockCursor(bool lockCursor)
    {
        lockState = lockCursor;

        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
    }
}