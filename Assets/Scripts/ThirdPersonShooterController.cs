using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using System;
using UnityEngine.Animations.Rigging;
using TMPro;
using ProjectY;
using UnityEngine.UI;

public class ThirdPersonShooterController : NetworkBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera followVirtualCamera;
    [SerializeField] private Animator animator;

    [SerializeField] private HealthBar healthBar;
    [SerializeField] private TextMeshProUGUI playerNameUI;

    [SerializeField] private Transform pistolObject;
    [SerializeField] private Transform rifleObject;

    [SerializeField]
    private float normalSensitivity = 1;

    [SerializeField]
    private float aimSensitivity = 0.5f;

    public bool useHitScan = false;

    [SerializeField] private Transform vfxHitRed;
    [SerializeField] private Transform vfxMuzzleRed;

    [SerializeField] private LayerMask mouseRaycastLayermask;
    public Transform bulletPrefab;
    public Transform spawnBulletPos;
    public Transform aimTarget;
    public Transform shoulderAimRig;
    public Transform RHandTarget;
    public Transform RHandRifleTarget;
    public Transform LHandIKRig;

    private Network3PController _3pcontroller;
    private DumbInputManager inputs;
    private Vector3 mouseWorldPosition = Vector3.zero;
    private Transform hitTrasnform = null;
    private Transform cameraTarget = null;

    [SerializeField] private NetworkVariable<PlayerStatus> playerStatus = new NetworkVariable<PlayerStatus>(
                            readPerm: NetworkVariableReadPermission.Everyone,
                            writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] private NetworkVariable<float> playerHealth = new NetworkVariable<float>(
                            readPerm: NetworkVariableReadPermission.Everyone,
                            writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private float clientHealth = 100f;
    [SerializeField] PlayerAnimState clientAnimationState = PlayerAnimState.Idle;
    [SerializeField] private bool clientIsAiming = false;
    [SerializeField] private bool usingRifle = false;
    private bool died = false;
    private bool reloading = false;
    private float realoadingTimer = 0;
    private float shootTimer = 0f;
    private string playerName;

    public AudioClip[] pistolShootingFX;
    public AudioClip[] rifleShootingFX;
    public AudioClip pistolReloadFX;
    public AudioClip rifleReloadFX;

    public float soundFXvolume = 1.0f;

    private WeaponStatus bigPistol = new WeaponStatus()
    {
        damage = 25,
        maxAmmo = 9,
        currAmmo = 9,
        fireMode = FireMode.SemiAuto,
        fireRateBPS = 2,
    };

    private WeaponStatus subMachinegun = new WeaponStatus()
    {
        damage = 5,
        maxAmmo = 35,
        currAmmo = 35,
        fireMode = FireMode.FullAuto,
        fireRateBPS = 10,
    };

    private WeaponStatus currentWeapon;

    private void Start()
    {
        bigPistol.shootingFX = pistolShootingFX;
        bigPistol.reloadFX = pistolReloadFX;
        subMachinegun.shootingFX = rifleShootingFX;
        subMachinegun.reloadFX = rifleReloadFX;

        if (IsServer)
        {
            playerHealth.Value = 100;
        }
        //if (IsOwner && IsClient)
        //{
        //    UpdateMyHealthServerRpc(100);
        //}
        cameraTarget = transform.Find("PlayerCameraRoot");
        _3pcontroller = GetComponent<Network3PController>();
        inputs = GetComponent<DumbInputManager>();
        animator = GetComponent<Animator>();
        currentWeapon = bigPistol;
    }

    private void Update()
    {
        ClientVisuals();

        if (IsOwner && IsClient)
        {
            playerName = MatchNetworkManager.Instance.UIManager.playerName;
            SetVirtualCameraTargets();
            ProcessInput();
        }

        UpdatePlayerStatus();

        if(clientHealth <= 0 && !died)
        {
            died = true;
            MatchNetworkManager.Instance.RespawnPlayer(OwnerClientId);
        }
        
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
            mouseWorldPosition = ray.GetPoint(25);
        }
        aimTarget.position = mouseWorldPosition;
    }

    private void ProcessInput()
    {
        shootTimer += Time.deltaTime;

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

            if (inputs.shoot && shootTimer > (float)(1f / currentWeapon.fireRateBPS) && currentWeapon.currAmmo > 0 && !reloading)
            {
                currentWeapon.currAmmo -= 1;

                shootTimer = 0;
                Vector3 aimDir = (mouseWorldPosition - spawnBulletPos.position).normalized;
                if (IsServer)
                {
                    if (currentWeapon.shootingFX.Length > 0)
                    {
                        var index = UnityEngine.Random.Range(0, currentWeapon.shootingFX.Length);
                        AudioSource.PlayClipAtPoint(currentWeapon.shootingFX[index], spawnBulletPos.position, soundFXvolume);
                        PlayGunShootFXClientRpc(index);
                    }
                    var muzzle = Instantiate(vfxMuzzleRed, spawnBulletPos.position, spawnBulletPos.rotation);
                    muzzle.GetComponent<NetworkObject>().Spawn();

                    // Estamos usando hitscan e estamos mirando em algo
                    if (useHitScan)
                    {
                        if(hitTrasnform != null)
                        {
                            if (hitTrasnform.tag == "Player")
                            {
                                UpdateEnemyHealthServerRpc(currentWeapon.damage, hitTrasnform.GetComponent<NetworkObject>().OwnerClientId);
                            }

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
                    if (currentWeapon.shootingFX.Length > 0)
                    {
                        var index = UnityEngine.Random.Range(0, currentWeapon.shootingFX.Length);
                        AudioSource.PlayClipAtPoint(currentWeapon.shootingFX[index], spawnBulletPos.position, soundFXvolume);
                        PlayGunShootFXServerRpc(index);
                    }
                    SpawnVfxMuzzleServerRpc(spawnBulletPos.position, spawnBulletPos.rotation);

                    // Estamos usando hitscan e estamos mirando em algo
                    if (useHitScan)
                    {
                        if (hitTrasnform != null)
                        {
                            if (hitTrasnform.tag == "Player")
                            {
                                UpdateEnemyHealthServerRpc(currentWeapon.damage, hitTrasnform.GetComponent<NetworkObject>().OwnerClientId);
                            }

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

        if (inputs.reload)
        {
            if (currentWeapon.shootingFX.Length > 0)
            {
                AudioSource.PlayClipAtPoint(currentWeapon.reloadFX, spawnBulletPos.position, soundFXvolume);
                if (IsServer)
                {
                    PlayGunReloadFXClientRpc();
                }
                else
                {
                    PlayGunReloadFXServerRpc();
                }
            }
            reloading = true;
            realoadingTimer = 0;
            StartCoroutine(WaitAndReload(currentWeapon.reloadFX.length));
        }

        if (reloading)
        {
            realoadingTimer += Time.deltaTime;
            var reloadUI = MatchNetworkManager.Instance.UIManager.ReloadUI;
            reloadUI.gameObject.SetActive(true);
            reloadUI.GetComponent<Image>().fillAmount = realoadingTimer / currentWeapon.reloadFX.length;
        }
        else
        {
            MatchNetworkManager.Instance.UIManager.ReloadUI.gameObject.SetActive(false);
        }

        if (inputs.escape)
        {
            inputs.SetCursorState(!inputs.cursorLocked);
        }

        if (inputs.usePistol)
        {
            usingRifle = false;
            subMachinegun.currAmmo = currentWeapon.currAmmo;
            currentWeapon = bigPistol;
        }

        if (inputs.useRifle)
        {
            usingRifle = true;
            bigPistol.currAmmo = currentWeapon.currAmmo;
            currentWeapon = subMachinegun;
        }

        MatchNetworkManager.Instance.UIManager.currAmmo.text = currentWeapon.currAmmo.ToString();
        MatchNetworkManager.Instance.UIManager.maxAmmo.text = currentWeapon.maxAmmo.ToString();

        if (usingRifle)
        {
            MatchNetworkManager.Instance.UIManager.PistolaUI.GetComponent<CanvasGroup>().alpha = 0.5f;
            MatchNetworkManager.Instance.UIManager.RifleUI.GetComponent<CanvasGroup>().alpha = 1f;
        }
        else
        {
            MatchNetworkManager.Instance.UIManager.PistolaUI.GetComponent<CanvasGroup>().alpha = 1f;
            MatchNetworkManager.Instance.UIManager.RifleUI.GetComponent<CanvasGroup>().alpha = 0.5f;
        }
    }

    [ServerRpc]
    private void PlayGunShootFXServerRpc(int soundidx)
    {
        AudioSource.PlayClipAtPoint(currentWeapon.shootingFX[soundidx], spawnBulletPos.position, soundFXvolume);
    }

    [ClientRpc]
    private void PlayGunShootFXClientRpc(int soundidx)
    {
        AudioSource.PlayClipAtPoint(currentWeapon.shootingFX[soundidx], spawnBulletPos.position, soundFXvolume);
    }

    [ServerRpc]
    private void PlayGunReloadFXServerRpc()
    {
        AudioSource.PlayClipAtPoint(currentWeapon.reloadFX, spawnBulletPos.position, soundFXvolume);
    }

    [ClientRpc]
    private void PlayGunReloadFXClientRpc()
    {
        AudioSource.PlayClipAtPoint(currentWeapon.reloadFX, spawnBulletPos.position, soundFXvolume);
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

    [ServerRpc]
    private void SpawnVfxMuzzleServerRpc(Vector3 position, Quaternion rotation)
    {
        var muzzle = Instantiate(vfxHitRed, position, rotation);
        muzzle.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateMyHealthServerRpc(float health)
    {
        playerHealth.Value = health;
    }

    [ServerRpc]
    public void UpdateEnemyHealthServerRpc(float damageTaken, ulong clientId)
    {
        var clientToDamage = NetworkManager.Singleton.ConnectedClients[clientId]
            .PlayerObject.GetComponent<ThirdPersonShooterController>();

        if (clientToDamage.playerHealth.Value > 0)
        {
            clientToDamage.playerHealth.Value -= damageTaken;
        }

        // execute method on a client getting hit
        NotifyHealthChangeClientRpc(damageTaken, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ClientRpc]
    public void NotifyHealthChangeClientRpc(float damageTaken, ClientRpcParams clientRpcParams = default)
    {

        Logger.Instance.LogInfo($"Client got hit by {damageTaken} damage");
    }

    public void SetVirtualCameraTargets()
    {
        if (aimVirtualCamera == null || followVirtualCamera == null)
        {
            aimVirtualCamera = MatchNetworkManager.Instance.aimVirtualCamera;
            aimVirtualCamera.Follow = cameraTarget;
            followVirtualCamera = MatchNetworkManager.Instance.followVirtualCamera;
            followVirtualCamera.Follow = cameraTarget;
        }
    }

    private void UpdatePlayerStatus()
    {
        if (IsOwner)
        {
            playerStatus.Value = new PlayerStatus()
            {
                animationState = clientAnimationState,
                aimTargetPos = aimTarget.position,
                isAiming = clientIsAiming,
                usingRifle = usingRifle,
                name = playerName,
            };
        }
        else
        {
            clientAnimationState = playerStatus.Value.animationState;
            aimTarget.position = playerStatus.Value.aimTargetPos;
            clientIsAiming = playerStatus.Value.isAiming;
            usingRifle = playerStatus.Value.usingRifle;
            playerName = playerStatus.Value.name.ToString();
        }

        clientHealth = playerHealth.Value;
    }



    private void ClientVisuals()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

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


        if (usingRifle)
        {
            rifleObject.gameObject.SetActive(true);
            pistolObject.gameObject.SetActive(false);

            RHandTarget.position = RHandRifleTarget.position;

            LHandIKRig.GetComponent< TwoBoneIKConstraint>().weight = 1;
        }
        else
        {
            rifleObject.gameObject.SetActive(false);
            pistolObject.gameObject.SetActive(true);

            RHandTarget.position = RHandTarget.parent.position;

            LHandIKRig.GetComponent<TwoBoneIKConstraint>().weight = 0;
        }


        // Ligando e desligando animmation rigging
        if (clientIsAiming)
        {
            shoulderAimRig.GetComponent<Rig>().weight = 1;
        }
        else
        {
            shoulderAimRig.GetComponent<Rig>().weight = 0;
        }

        healthBar.UpdateHealthBar(100, clientHealth);
        playerNameUI.text = playerName;
    }

    IEnumerator WaitAndReload(float waitFor)
    {
        yield return new WaitForSeconds(waitFor);
        currentWeapon.currAmmo = currentWeapon.maxAmmo;
        reloading = false;
    }
}
