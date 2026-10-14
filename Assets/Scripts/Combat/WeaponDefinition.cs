using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "UNSELF/Combat/Weapon")]
public class WeaponDefinition : ScriptableObject
{
    [SerializeField] private float damage = 30f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireInterval = 0.18f;
    [SerializeField] private int magazineSize = 8;
    [SerializeField] private int startingReserveAmmo = 32;
    [SerializeField] private float reloadDuration = 1.3f;

    public float Damage => damage;
    public float Range => range;
    public float FireInterval => fireInterval;
    public int MagazineSize => magazineSize;
    public int StartingReserveAmmo => startingReserveAmmo;
    public float ReloadDuration => reloadDuration;
}