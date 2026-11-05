
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private WeaponRecoil weaponRecoil;
    [SerializeField] private MuzzleFlash muzzleFlash;

    private PlayerEquipment playerEquipment;
    private PlayerMovement playerMovement;
    private TraversalMotor traversalMotor;

    private InputAction attackAction;
    private InputAction reloadAction;

    private readonly Dictionary<EquipmentEntry, AmmoState> ammoStates = new();

    private WeaponDefinition activeWeapon;
    private AmmoState activeAmmo;

    private float nextFireTime;
    private float reloadEndTime;
    private bool isReloading;

    private sealed class AmmoState
    {
        public int Magazine;
        public int Reserve;

        public AmmoState(WeaponDefinition weapon)
        {
            Magazine = weapon.MagazineSize;
            Reserve = weapon.StartingReserveAmmo;
        }
    }

    public event Action Fired;

    public int CurrentAmmo => activeAmmo != null ? activeAmmo.Magazine : 0;
    public int ReserveAmmo => activeAmmo != null ? activeAmmo.Reserve : 0;
    public bool IsReloading => isReloading;
    public bool IsWeaponEquipped => activeWeapon != null;

    private void Awake()
    {
        playerEquipment = GetComponent<PlayerEquipment>();
        playerMovement = GetComponent<PlayerMovement>();
        traversalMotor = GetComponent<TraversalMotor>();

        attackAction = GameControls.GetPlayerAction("Attack");
        reloadAction = GameControls.GetPlayerAction("Reload");

        if (playerEquipment == null)
            Debug.LogError("PlayerEquipment component is missing.", this);
    }

    private void OnEnable()
    {
        if (playerEquipment != null)
            playerEquipment.EquipmentChanged += HandleEquipmentChanged;
    }

    private void Start()
    {
        RefreshEquippedWeapon();
    }

    private void OnDisable()
    {
        if (playerEquipment != null)
            playerEquipment.EquipmentChanged -= HandleEquipmentChanged;
    }

    private void Update()
    {
        if (!IsWeaponEquipped || activeAmmo == null)
            return;

        if (isReloading)
        {
            if (Time.time >= reloadEndTime)
                CompleteReload();

            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (playerMovement != null &&
            (!playerMovement.enabled || playerMovement.IsSliding))
            return;

        if (traversalMotor != null && traversalMotor.IsTraversing)
            return;

        if (reloadAction.WasPressedThisFrame())
            StartReload();

        if (!isReloading && attackAction.WasPressedThisFrame())
            TryFire();
    }

    private void HandleEquipmentChanged(EquipmentEntry entry)
    {
        RefreshEquippedWeapon();
    }

    private void RefreshEquippedWeapon()
    {
        // Смена экипировки отменяет незавершённую перезарядку.
        isReloading = false;

        if (muzzleFlash != null)
            muzzleFlash.Stop();

        activeWeapon = playerEquipment != null ? playerEquipment.CurrentWeapon : null;
        activeAmmo = null;

        if (activeWeapon == null)
            return;

        EquipmentEntry entry = playerEquipment.CurrentEntry;

        if (!ammoStates.TryGetValue(entry, out activeAmmo))
        {
            activeAmmo = new AmmoState(activeWeapon);
            ammoStates.Add(entry, activeAmmo);
        }
    }

    private void TryFire()
    {
        if (!IsWeaponEquipped || activeAmmo == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (activeAmmo.Magazine <= 0)
        {
            AudioService.Play("weapon_empty");
            nextFireTime = Time.time + activeWeapon.FireInterval;
            return;
        }

        activeAmmo.Magazine--;
        nextFireTime = Time.time + activeWeapon.FireInterval;

        AudioService.Play("weapon_fire");

        if (muzzleFlash != null)
            muzzleFlash.Play();

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            activeWeapon.Range,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(activeWeapon.Damage);
        }

        ApplyRecoil();
        Fired?.Invoke();
    }

    private void StartReload()
    {
        if (!IsWeaponEquipped || activeAmmo == null)
            return;

        if (activeAmmo.Magazine >= activeWeapon.MagazineSize ||
            activeAmmo.Reserve <= 0)
            return;

        isReloading = true;
        reloadEndTime = Time.time + activeWeapon.ReloadDuration;
    }

    private void CompleteReload()
    {
        int missingAmmo = activeWeapon.MagazineSize - activeAmmo.Magazine;
        int loadedAmmo = Mathf.Min(missingAmmo, activeAmmo.Reserve);

        activeAmmo.Magazine += loadedAmmo;
        activeAmmo.Reserve -= loadedAmmo;

        isReloading = false;
    }

    private void ApplyRecoil()
    {
        float yaw = UnityEngine.Random.Range(
            -activeWeapon.RecoilYaw,
            activeWeapon.RecoilYaw
        );

        if (playerLook != null)
            playerLook.AddRecoil(activeWeapon.RecoilPitch, yaw);

        if (weaponRecoil != null)
            weaponRecoil.Kick(activeWeapon);
    }
}
