using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Vector3 currentPositionOffset;
    private Vector3 targetPositionOffset;

    private Vector3 currentRotationOffset;
    private Vector3 targetRotationOffset;

    private float recoilSpeed;
    private float returnSpeed;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float recoilBlend = 1f - Mathf.Exp(-recoilSpeed * Time.deltaTime);
        float returnBlend = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);

        currentPositionOffset = Vector3.Lerp(currentPositionOffset, targetPositionOffset, recoilBlend);
        currentRotationOffset = Vector3.Lerp(currentRotationOffset, targetRotationOffset, recoilBlend);

        targetPositionOffset = Vector3.Lerp(targetPositionOffset, Vector3.zero, returnBlend);
        targetRotationOffset = Vector3.Lerp(targetRotationOffset, Vector3.zero, returnBlend);

        transform.localPosition = baseLocalPosition + currentPositionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentRotationOffset);
    }

    public void Kick(WeaponDefinition weapon)
    {
        recoilSpeed = weapon.VisualRecoilSpeed;
        returnSpeed = weapon.VisualReturnSpeed;

        targetPositionOffset += new Vector3(
            0f,
            weapon.VisualKickUp,
            -weapon.VisualKickback
        );

        targetRotationOffset += new Vector3(
            -weapon.VisualKickAngle,
            0f,
            0f
        );
    }
}