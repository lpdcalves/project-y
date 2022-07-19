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
    public float bulletSpeed = 100f;
    [SerializeField] private Transform vfxHitRed;

    private void Awake()
    {
        bulletRB = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        bulletRB.velocity = transform.forward * bulletSpeed;
        if (IsServer)
            Destroy(gameObject, 10);
        else
            DestroyBulletServerRpc(10);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            Instantiate(vfxHitRed, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else 
        {
            SpawnVfxHitServerRpc();
            DestroyBulletServerRpc(0);
        }
    }

    [ServerRpc(RequireOwnership=false)]
    private void DestroyBulletServerRpc(float ttd)
    {
        Destroy(gameObject, ttd);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnVfxHitServerRpc()
    {
        var hit = Instantiate(vfxHitRed, transform.position, Quaternion.identity);
        hit.GetComponent<NetworkObject>().Spawn();
    }
}
