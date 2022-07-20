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
    public Vector3 aimTargetPos;
    public bool isAiming;
    public bool usingRifle;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref animationState);
        serializer.SerializeValue(ref aimTargetPos);
        serializer.SerializeValue(ref isAiming);
        serializer.SerializeValue(ref usingRifle);
    }
}
