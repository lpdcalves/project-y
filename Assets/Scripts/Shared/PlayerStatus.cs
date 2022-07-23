using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerAnimState
{
    Idle,
    AimPistol,
    AimRifle,
}

public enum FireMode
{
    SemiAuto,
    BurstFire,
    FullAuto,
}

public struct WeaponStatus
{
    public float damage;
    public int maxAmmo;
    public int currAmmo;
    public FireMode fireMode;
    public int fireRateBPS;
    public AudioClip[] shootingFX;
    public AudioClip reloadFX;
}

public struct PlayerStatus : INetworkSerializable
{

    public PlayerAnimState animationState;
    public Vector3 aimTargetPos;
    public bool isAiming;
    public bool usingRifle;
    public FixedString64Bytes name;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref animationState);
        serializer.SerializeValue(ref aimTargetPos);
        serializer.SerializeValue(ref isAiming);
        serializer.SerializeValue(ref usingRifle);
        serializer.SerializeValue(ref name);
    }
}
