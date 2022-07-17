using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using System;

public class ThirdPersonShooterController : NetworkBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera aimVirtualCamera;

    [SerializeField]
    private CinemachineVirtualCamera followVirtualCamera;

    [SerializeField]
    private float normalSensitivity = 1;

    [SerializeField]
    private float aimSensitivity = 0.5f;

    [SerializeField] private LayerMask mouseRaycastLayermask;
    public Transform bulletPrefab;
    public Transform spawnBulletPos;

    private Network3PController _3pcontroller;
    private DumbInputManager inputs;
    private Vector3 mouseWorldPosition = Vector3.zero;

    private void Start()
    {
        SetVirtualCameras();
        _3pcontroller = GetComponent<Network3PController>();
        inputs = GetComponent<DumbInputManager>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            CalculateAimRaycast();
            ProcessInput();
        }
    }

    private void CalculateAimRaycast()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, mouseRaycastLayermask))
        {
            mouseWorldPosition = raycastHit.point;
        }
        else
        {
            mouseWorldPosition = ray.GetPoint(100);
        }
    }

    private void ProcessInput()
    {
        if (inputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            _3pcontroller.SetSensitivity(aimSensitivity);
            _3pcontroller.SetRotateOnMove(false);

            Vector3 worldAimTarget = mouseWorldPosition;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);

            if (inputs.shoot)
            {
                Vector3 aimDir = (mouseWorldPosition - spawnBulletPos.position).normalized;
                if (IsServer)
                {
                    var bullet = Instantiate(bulletPrefab, spawnBulletPos.position, Quaternion.LookRotation(aimDir, Vector3.up));
                    bullet.GetComponent<NetworkObject>().Spawn();
                }
                else
                {
                    SpawnBulletServerRpc(aimDir);
                }

                inputs.shoot = false;
            }
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
            _3pcontroller.SetSensitivity(normalSensitivity);
            _3pcontroller.SetRotateOnMove(true);
        }
    }

    [ServerRpc]
    private void SpawnBulletServerRpc(Vector3 aimDir)
    {
        var bullet = Instantiate(bulletPrefab, spawnBulletPos.position, Quaternion.LookRotation(aimDir, Vector3.up));
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    public void SetVirtualCameraTargets(Transform cameraTarget)
    {
        SetVirtualCameras();
        aimVirtualCamera.Follow = cameraTarget;
        followVirtualCamera.Follow = cameraTarget;
    }

    void SetVirtualCameras()
    {
        if (aimVirtualCamera == null)
            aimVirtualCamera = GameObject.Find("PlayerAimCamera").GetComponent<CinemachineVirtualCamera>();
        if (followVirtualCamera == null)
            followVirtualCamera = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
    }
}
