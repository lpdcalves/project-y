using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumbInputManager : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;
	public bool jump;
	public bool sprint;
	public bool aim;
	public bool shoot;
	public bool escape;
	public bool usePistol;
	public bool useRifle;
	public bool reload;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = false;
	public bool cursorInputForLook = true;

    private void Start()
    {
		SetCursorState(true);
	}


    private void Update()
    {
        if (cursorLocked)
        {
			move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			look = new Vector2(Input.GetAxis("Mouse X"), -1 * Input.GetAxis("Mouse Y"));
			jump = Input.GetKeyDown(KeyCode.Space);
			sprint = Input.GetKey(KeyCode.LeftShift);
			aim = Input.GetMouseButton(1); // Right click
			shoot = Input.GetMouseButton(0);
			reload = Input.GetKeyDown(KeyCode.R);
		}
        else
        {
			move = Vector2.zero;
			look = Vector2.zero;
		}
		 // Left click
		escape = Input.GetKeyDown(KeyCode.Escape);
		usePistol = Input.GetKeyDown(KeyCode.Alpha1);
		useRifle = Input.GetKeyDown(KeyCode.Alpha2);
	}

	public void SetCursorState(bool newState)
	{
		cursorLocked = newState;
		if (cursorLocked)
        {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
        else
        {
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
			
	}

    public void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
			SetCursorState(true);
        }
    }
}