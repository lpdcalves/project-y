using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private float walkSpeed = 3.5f;

    [SerializeField]
    private Vector2 posRange = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<float> forwardBackPosition = new NetworkVariable<float>();

    [SerializeField]
    private NetworkVariable<float> leftRightPosition = new NetworkVariable<float>();

    // client caching to save on updates
    private float oldForwardBackPosition;
    private float oldLeftRightPosition;

    private void Start()
    {
        transform.position = GetRandomPositionOnPlane();
    }

    private void Update()
    {
        if (IsServer)
        {
            UpdateServer();
        }

        if (IsClient && IsOwner)
        {
            UpdateClient();
        }
    }

    private void UpdateServer()
    {
        transform.position = new Vector3(transform.position.x + leftRightPosition.Value,
                                        transform.position.y,
                                        transform.position.z + forwardBackPosition.Value);
    }

    private void UpdateClient()
    {
        float forwardBackward = 0;
        float leftRight = 0;

        if (Input.GetKey(KeyCode.W))
        {
            forwardBackward += walkSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            forwardBackward -= walkSpeed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            leftRight -= walkSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            leftRight += walkSpeed;
        }

        if(oldForwardBackPosition != forwardBackward || oldLeftRightPosition != leftRight)
        {
            oldForwardBackPosition = forwardBackward;
            oldLeftRightPosition = leftRight;

            // updating client position
            UpdateClientPositionServerRpc(forwardBackward, leftRight);
        }
    }

    [ServerRpc]
    public void UpdateClientPositionServerRpc(float forwardBackward, float leftRight)
    {
        forwardBackPosition.Value = forwardBackward;
        leftRightPosition.Value = leftRight;
    }

    private Vector3 GetRandomPositionOnPlane()
    {
        return new Vector3(Random.Range(posRange.x, posRange.y), 0f, Random.Range(posRange.x, posRange.y));
    }
}
