using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] private WeaponDefinition weapon;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask hitMask = ~0;

    private int currentAmmo;
    private int reserveAmmo;

    private float nextFireTime;
    private float reloadEndTime;
    private bool isReloading;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        currentAmmo = weapon.MagazineSize;
        reserveAmmo = weapon.StartingReserveAmmo;
    }

    private void Update()
    {
        if (isReloading)
        {
            if (Time.time >= reloadEndTime)
                CompleteReload();

            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            StartReload();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime || currentAmmo <= 0)
            return;

        currentAmmo--;
        nextFireTime = Time.time + weapon.FireInterval;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, weapon.Range, hitMask, QueryTriggerInteraction.Ignore))
            return;

        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

        damageable?.TakeDamage(weapon.Damage);
    }

    private void StartReload()
    {
        if (currentAmmo >= weapon.MagazineSize || reserveAmmo <= 0)
            return;

        isReloading = true;
        reloadEndTime = Time.time + weapon.ReloadDuration;
    }

    private void CompleteReload()
    {
        int missingAmmo = weapon.MagazineSize - currentAmmo;
        int loadedAmmo = Mathf.Min(missingAmmo, reserveAmmo);

        currentAmmo += loadedAmmo;
        reserveAmmo -= loadedAmmo;

        isReloading = false;
    }
}