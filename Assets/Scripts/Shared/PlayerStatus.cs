using Unity.Netcode;
using UnityEngine;

public enum PlayerAnimState
{
    Idle,
    Walk,
    Run,
    ReverseWalk,
}

public struct PlayerStatus : INetworkSerializable
{

    public PlayerAnimState animationState;
    public float health;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref animationState);
        serializer.SerializeValue(ref health);
    }
}
