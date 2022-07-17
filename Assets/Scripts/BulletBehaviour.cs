using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(ClientNetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class BulletBehaviour : NetworkBehaviour
{
    private Rigidbody bulletRB;

    private void Awake()
    {
        bulletRB = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        float speed = 10f;
        bulletRB.velocity = transform.forward * speed;
        Destroy(gameObject, 10);
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
}
