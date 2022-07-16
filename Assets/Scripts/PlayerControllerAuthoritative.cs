using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerControllerAuthoritative : NetworkBehaviour
{
    [SerializeField]
    private float speed = 3.5f;

    [SerializeField]
    private float rotationSpeed = 200f;

    [SerializeField]
    private float clientHealth = 100f;

    [SerializeField]
    PlayerAnimState clientAnimationState;

    [SerializeField]
    private NetworkVariable<PlayerStatus> playerStatus = new NetworkVariable<PlayerStatus>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner);


    private CharacterController characterController;
    private Animator animator;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        animator.fireEvents = false;
    }

    private void Start()
    {
        if( IsClient && IsOwner)
        {
            PlayerCameraFollow.Instance.FollowPlayer(transform.Find("PlayerCameraRoot"));
        }
    }

    private void Update()
    {
        UpdatePlayerStatus();

        if (IsClient && IsOwner)
        {
            ClientInput();
        }

        ClientVisuals();
    }

    private void UpdatePlayerStatus()
    {
        if (IsOwner)
        {
            playerStatus.Value = new PlayerStatus()
            {
                animationState = clientAnimationState,
                health = clientHealth
            };
        }
        else
        {
            clientAnimationState = playerStatus.Value.animationState;
            clientHealth = playerStatus.Value.health;
        }
    }

    private void ClientInput()
    {
        // Player input changes
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.LeftShift) && forwardInput > 0) forwardInput = 2;
        Vector3 inputPosition = direction * forwardInput;

        // Client moves itself
        characterController.SimpleMove(inputPosition * speed);
        transform.Rotate(inputRotation * rotationSpeed * Time.deltaTime, Space.World);

        // Player state changes
        if (forwardInput > 0 && forwardInput <= 1)
        {
            UpdatePlayerAnimState(PlayerAnimState.Walk);
        }
        else if (forwardInput > 1)
        {
            UpdatePlayerAnimState(PlayerAnimState.Run);
        }
        else if(forwardInput < 0)
        {
            UpdatePlayerAnimState(PlayerAnimState.ReverseWalk);
        }
        else
        {
            UpdatePlayerAnimState(PlayerAnimState.Idle);
        }
    }

    private void ClientVisuals()
    {
        if(clientAnimationState == PlayerAnimState.Walk)
        {
            animator.SetFloat("Walk", 1);
        }
        else if (clientAnimationState == PlayerAnimState.Run)
        {
            animator.SetFloat("Walk", 2);
        }
        else if (clientAnimationState == PlayerAnimState.ReverseWalk)
        {
            animator.SetFloat("Walk", -1);
        }
        else
        {
            animator.SetFloat("Walk", 0);
        }

    }

    public void UpdatePlayerAnimState(PlayerAnimState newState)
    {
        clientAnimationState = newState;
    }
}
