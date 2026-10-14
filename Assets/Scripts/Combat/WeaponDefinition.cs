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

    [Header("Recoil")]
    [SerializeField] private float recoilPitch = 1.25f;
    [SerializeField] private float recoilYaw = 0.3f;
    [SerializeField] private float visualKickback = 0.06f;
    [SerializeField] private float visualKickUp = 0.025f;
    [SerializeField] private float visualKickAngle = 5f;
    [SerializeField] private float visualRecoilSpeed = 22f;
    [SerializeField] private float visualReturnSpeed = 12f;

    public float Damage => damage;
    public float Range => range;
    public float FireInterval => fireInterval;
    public int MagazineSize => magazineSize;
    public int StartingReserveAmmo => startingReserveAmmo;
    public float ReloadDuration => reloadDuration;

    public float RecoilPitch => recoilPitch;
    public float RecoilYaw => recoilYaw;
    public float VisualKickback => visualKickback;
    public float VisualKickUp => visualKickUp;
    public float VisualKickAngle => visualKickAngle;
    public float VisualRecoilSpeed => visualRecoilSpeed;
    public float VisualReturnSpeed => visualReturnSpeed;
}