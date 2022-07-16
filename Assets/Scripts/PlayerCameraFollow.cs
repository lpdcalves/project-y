using Cinemachine;
using Core.Singletons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraFollow : Singleton<PlayerCameraFollow>
{
    private CinemachineVirtualCamera cinemachineVirtualCamera;

    private void Awake()
    {
        cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    public void FollowPlayer(Transform transform)
    {
        cinemachineVirtualCamera.enabled = true;
        cinemachineVirtualCamera.Follow = transform;
        var perlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        perlin.m_AmplitudeGain = 0.5f;
        perlin.m_FrequencyGain = 0.5f;
    }
}
