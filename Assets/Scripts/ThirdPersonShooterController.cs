using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using System;
using UnityEngine.Animations.Rigging;

public class ThirdPersonShooterController : NetworkBehaviour
{
    private CinemachineVirtualCamera aimVirtualCamera;
    private CinemachineVirtualCamera followVirtualCamera;

    [SerializeField]
    private float normalSensitivity = 1;

    [SerializeField]
    private float aimSensitivity = 0.5f;

    public bool useHitScan = false;

    [SerializeField] private Transform vfxHitRed;

    [SerializeField] private LayerMask mouseRaycastLayermask;
    private Animator animator;
    public Transform bulletPrefab;
    public Transform spawnBulletPos;
    public Transform aimTarget;
    public Transform shoulderAimRig;

    private Network3PController _3pcontroller;
    private DumbInputManager inputs;
    private Vector3 mouseWorldPosition = Vector3.zero;
    private Transform hitTrasnform = null;

    [SerializeField] private NetworkVariable<PlayerStatus> playerStatus = new NetworkVariable<PlayerStatus>(
                            readPerm: NetworkVariableReadPermission.Everyone,
                            writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] private float clientHealth = 100f;
    [SerializeField] PlayerAnimState clientAnimationState = PlayerAnimState.Idle;
    [SerializeField] private bool clientIsAiming = false;
    [SerializeField] private bool usingRifle = false;

    private void Start()
    {
        SetVirtualCameras();
        _3pcontroller = GetComponent<Network3PController>();
        inputs = GetComponent<DumbInputManager>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        ClientVisuals();

        if (IsOwner && IsClient)
        {
            ProcessInput();
        }

        UpdatePlayerStatus();
        
    }

    private void CalculateAimRaycast()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        hitTrasnform = null;
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, mouseRaycastLayermask))
        {
            mouseWorldPosition = raycastHit.point;
            hitTrasnform = raycastHit.transform;
        }
        else
        {
            mouseWorldPosition = ray.GetPoint(10);
        }
        aimTarget.position = mouseWorldPosition;
    }

    private void ProcessInput()
    {
        if (inputs.aim)
        {
            clientIsAiming = true;
            CalculateAimRaycast();
            if (usingRifle)
            {
                clientAnimationState = PlayerAnimState.AimRifle;
            }
            else
            {
                clientAnimationState = PlayerAnimState.AimPistol;
            }

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
                    // Estamos usando hitscan e estamos mirando em algo
                    if (useHitScan)
                    {
                        if(hitTrasnform != null)
                        {
                            var hit = Instantiate(vfxHitRed, mouseWorldPosition, Quaternion.identity);
                            hit.GetComponent<NetworkObject>().Spawn();
                        }
                    }
                    else 
                    { 
                        var bullet = Instantiate(bulletPrefab, spawnBulletPos.position, Quaternion.LookRotation(aimDir, Vector3.up));
                        bullet.GetComponent<NetworkObject>().Spawn();
                    }
                }
                else
                {
                    // Estamos usando hitscan e estamos mirando em algo
                    if (useHitScan)
                    {
                        if (hitTrasnform != null)
                        {
                            SpawnVfxHitServerRpc(mouseWorldPosition);
                        }
                    }
                    else
                    {
                        SpawnBulletServerRpc(aimDir);
                    }
                }

                inputs.shoot = false;
            }
        }
        else
        {
            clientIsAiming = false;
            clientAnimationState = PlayerAnimState.Idle;

            aimVirtualCamera.gameObject.SetActive(false);
            _3pcontroller.SetSensitivity(normalSensitivity);
            _3pcontroller.SetRotateOnMove(true);
        }

        if (inputs.escape)
        {
            inputs.SetCursorState(!inputs.cursorLocked);
        }

        if (inputs.usePistol)
        {
            usingRifle = false;
        }

        if (inputs.useRifle)
        {
            usingRifle = true;
        }
    }

    [ServerRpc]
    private void SpawnBulletServerRpc(Vector3 aimDir)
    {
        var bullet = Instantiate(bulletPrefab, spawnBulletPos.position, Quaternion.LookRotation(aimDir, Vector3.up));
        bullet.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    private void SpawnVfxHitServerRpc(Vector3 position)
    {
        var hit = Instantiate(vfxHitRed, position, Quaternion.identity);
        hit.GetComponent<NetworkObject>().Spawn();
    }

    public void SetVirtualCameraTargets(Transform cameraTarget)
    {
        SetVirtualCameras();
        aimVirtualCamera.Follow = cameraTarget;
        followVirtualCamera.Follow = cameraTarget;
    }

    void SetVirtualCameras()
    {
        if (IsOwner) 
        {
            if (aimVirtualCamera == null)
                aimVirtualCamera = GameObject.Find("PlayerAimCamera").GetComponent<CinemachineVirtualCamera>();
            if (followVirtualCamera == null)
                followVirtualCamera = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
        }
        
    }

    private void UpdatePlayerStatus()
    {
        if (IsOwner)
        {
            playerStatus.Value = new PlayerStatus()
            {
                animationState = clientAnimationState,
                health = clientHealth,
                aimTargetPos = aimTarget.position,
                isAiming = clientIsAiming,
            };
        }
        else
        {
            clientAnimationState = playerStatus.Value.animationState;
            clientHealth = playerStatus.Value.health;
            aimTarget.position = playerStatus.Value.aimTargetPos;
            clientIsAiming = playerStatus.Value.isAiming;
        }
    }

    private void ClientVisuals()
    {
        
        if (clientAnimationState == PlayerAnimState.AimPistol)
        {
            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));
            animator.SetLayerWeight(2, 0f);
        }
        else if (clientAnimationState == PlayerAnimState.AimRifle)
        {
            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 1f, Time.deltaTime * 10f));
            animator.SetLayerWeight(1, 0f);
        }
        else
        {
            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
            animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(2), 0f, Time.deltaTime * 10f));
        }

        //if (clientAnimationState == PlayerAnimState.AimRifle)
        //{
        //    animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));
        //    //animator.SetLayerWeight(1, 1f);
        //}
        //else
        //{
        //    animator.SetLayerWeight(2, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
        //    //animator.SetLayerWeight(1, 0f);
        //}

        // Ligando e desligando animmation rigging
        if (clientIsAiming)
        {
            shoulderAimRig.GetComponent<Rig>().weight = 1;
        }
        else
        {
            shoulderAimRig.GetComponent<Rig>().weight = 0;
        }
    }
}
