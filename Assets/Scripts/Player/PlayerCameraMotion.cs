using UnityEngine;

public class PlayerCameraMotion : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera playerCamera;

    [Header("Walking")]
    [SerializeField] private float walkBobFrequency = 7.5f;
    [SerializeField] private float sprintBobFrequency = 10f;
    [SerializeField] private float horizontalBobAmplitude = 0.012f;
    [SerializeField] private float verticalBobAmplitude = 0.018f;
    [SerializeField] private float bobReturnSpeed = 10f;

    [Header("Movement Inertia")]
    [SerializeField] private float inertiaScale = 0.0015f;
    [SerializeField] private float maximumInertiaOffset = 0.025f;
    [SerializeField] private float inertiaResponseSpeed = 8f;

    [Header("Strafe Tilt")]
    [SerializeField] private float strafeTiltAngle = 1.8f;
    [SerializeField] private float tiltResponseSpeed = 7f;

    [Header("Sprint FOV")]
    [SerializeField] private float sprintFovIncrease = 3f;
    [SerializeField] private float fovResponseSpeed = 5f;

    [Header("Landing")]
    [SerializeField] private float landingImpactThreshold = 4f;
    [SerializeField] private float landingImpactScale = 0.012f;
    [SerializeField] private float maximumLandingDip = 0.1f;
    [SerializeField] private float landingRecoverySpeed = 9f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 currentBobOffset;
    private Vector3 currentInertiaOffset;
    private Vector3 previousLocalVelocity;

    private float baseFieldOfView;
    private float bobPhase;
    private float landingOffset;
    private float currentRoll;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        if (playerCamera != null)
            baseFieldOfView = playerCamera.fieldOfView;
    }

    private void OnEnable()
    {
        playerMovement.Landed += HandleLanding;
    }

    private void OnDisable()
    {
        playerMovement.Landed -= HandleLanding;
    }

    private void LateUpdate()
    {
        Vector3 localVelocity = playerMovement.transform.InverseTransformDirection(playerMovement.HorizontalVelocity);

        UpdateBob();
        UpdateMovementInertia(localVelocity);
        UpdateStrafeTilt(localVelocity);
        UpdateSprintFov();
        UpdateLanding();

        transform.localPosition = baseLocalPosition + currentBobOffset + currentInertiaOffset + Vector3.up * landingOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, currentRoll);

        previousLocalVelocity = localVelocity;
    }

    private void UpdateBob()
    {
        bool isMoving = playerMovement.IsGrounded && playerMovement.HorizontalSpeed > 0.1f;
        Vector3 targetOffset = Vector3.zero;

        if (isMoving)
        {
            float frequency = Mathf.Lerp(walkBobFrequency, sprintBobFrequency, playerMovement.SpeedRatio);
            bobPhase += frequency * Time.deltaTime;

            float intensity = Mathf.Lerp(0.65f, 1f, playerMovement.SpeedRatio);
            float horizontalOffset = Mathf.Sin(bobPhase) * horizontalBobAmplitude * intensity;
            float verticalOffset = Mathf.Sin(bobPhase * 2f) * verticalBobAmplitude * intensity;

            targetOffset = new Vector3(horizontalOffset, verticalOffset, 0f);
        }

        float blend = 1f - Mathf.Exp(-bobReturnSpeed * Time.deltaTime);
        currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, blend);
    }

    private void UpdateMovementInertia(Vector3 localVelocity)
    {
        if (Time.deltaTime <= 0f)
            return;

        Vector3 localAcceleration = (localVelocity - previousLocalVelocity) / Time.deltaTime;

        float targetX = Mathf.Clamp(-localAcceleration.x * inertiaScale, -maximumInertiaOffset, maximumInertiaOffset);
        float targetZ = Mathf.Clamp(-localAcceleration.z * inertiaScale, -maximumInertiaOffset, maximumInertiaOffset);

        Vector3 targetOffset = new Vector3(targetX, 0f, targetZ);
        float blend = 1f - Mathf.Exp(-inertiaResponseSpeed * Time.deltaTime);

        currentInertiaOffset = Vector3.Lerp(currentInertiaOffset, targetOffset, blend);
    }

    private void UpdateStrafeTilt(Vector3 localVelocity)
    {
        float lateralRatio = playerMovement.HorizontalSpeed > 0.01f
            ? Mathf.Clamp(localVelocity.x / playerMovement.HorizontalSpeed, -1f, 1f)
            : 0f;

        float targetRoll = -lateralRatio * strafeTiltAngle * playerMovement.SpeedRatio;
        float blend = 1f - Mathf.Exp(-tiltResponseSpeed * Time.deltaTime);

        currentRoll = Mathf.Lerp(currentRoll, targetRoll, blend);
    }

    private void UpdateSprintFov()
    {
        if (playerCamera == null)
            return;

        float sprintAmount = playerMovement.IsSprinting ? playerMovement.SprintRatio : 0f;
        float targetFieldOfView = baseFieldOfView + sprintFovIncrease * sprintAmount;
        float blend = 1f - Mathf.Exp(-fovResponseSpeed * Time.deltaTime);

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFieldOfView, blend);
    }

    private void UpdateLanding()
    {
        float blend = 1f - Mathf.Exp(-landingRecoverySpeed * Time.deltaTime);
        landingOffset = Mathf.Lerp(landingOffset, 0f, blend);
    }

    private void HandleLanding(float impactSpeed)
    {
        float impact = Mathf.Max(0f, impactSpeed - landingImpactThreshold);
        landingOffset = -Mathf.Min(impact * landingImpactScale, maximumLandingDip);
    }
}