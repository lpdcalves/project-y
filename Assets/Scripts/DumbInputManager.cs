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

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = false;
	public bool cursorInputForLook = true;

    private void Update()
    {
		move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		look = new Vector2(Input.GetAxis("Mouse X"), -1 * Input.GetAxis("Mouse Y"));
		jump = Input.GetKeyDown(KeyCode.Space);
		sprint = Input.GetKey(KeyCode.LeftShift);
	}
}