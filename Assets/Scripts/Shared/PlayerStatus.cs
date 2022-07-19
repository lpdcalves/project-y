using Unity.Netcode;
using UnityEngine;

public enum PlayerAnimState
{
    Idle,
    AimPistol,
    AimRifle,
}

public struct PlayerStatus : INetworkSerializable
{

    public PlayerAnimState animationState;
    public float health;
    public Vector3 aimTargetPos;
    public bool isAiming;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref animationState);
        serializer.SerializeValue(ref health);
        serializer.SerializeValue(ref aimTargetPos);
        serializer.SerializeValue(ref isAiming);
    }
}
