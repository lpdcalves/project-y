using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VfxParticleNetworkDeleter : NetworkBehaviour
{
    public void OnParticleSystemStopped()
    {
        if (IsServer)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyParticleServerRpc(0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyParticleServerRpc(float ttd)
    {
        Destroy(gameObject, ttd);
    }
}
