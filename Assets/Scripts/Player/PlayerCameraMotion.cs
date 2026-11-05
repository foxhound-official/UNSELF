using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerCameraMotion : MonoBehaviour
{
    private static readonly int SprintStateHash = Animator.StringToHash("VM_Sprint");
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera playerCamera;

    [Header("Walking")]
    [SerializeField] private float walkBobFrequency = 7.5f;
    [SerializeField] private float sprintBobFrequency = 10f;
    [SerializeField] private float horizontalBobAmplitude = 0.012f;
    [SerializeField] private float verticalBobAmplitude = 0.018f;
    [SerializeField] private float bobReturnSpeed = 10f;

    [Header("Sprint Bob Synchronization")]
    [SerializeField] private Animator viewmodelAnimator;
    [SerializeField, Range(0.5f, 1.5f)] private float sprintVerticalBobSpeedMultiplier = 0.9f;

    [SerializeField, Range(0f, 1f)]
    private float sprintBobPhaseOffset = 0f;

    [SerializeField, Min(0.1f)]
    private float sprintBobBlendSpeed = 10f;

    [Header("Sprint")]
    [SerializeField] private float sprintBobAmplitudeMultiplier = 1.35f;
    [SerializeField] private float sprintForwardPitch = 1.5f;
    [SerializeField] private float sprintCameraDrop = 0.015f;
    [SerializeField] private float sprintPresentationSpeed = 7f;

    [Header("Slide")]
    [SerializeField, Range(0f, 10f)] private float slideRollAngle = 3f;
    [SerializeField, Min(0.1f)] private float slideRollResponseSpeed = 10f;

    [Header("Traversal")]
    [SerializeField] private float vaultCameraDip = 0.06f;
    [SerializeField] private float vaultPitch = 3f;
    [SerializeField] private float vaultRollAngle = 2.5f;
    [SerializeField] private float vaultPresentationDuration = 0.4f;

    [FormerlySerializedAs("mantleCameraDip")]
    [SerializeField] private float ledgeClimbCameraDip = 0.12f;

    [FormerlySerializedAs("mantlePitch")]
    [SerializeField] private float ledgeClimbPitch = 5f;

    [FormerlySerializedAs("mantlePresentationDuration")]
    [SerializeField] private float ledgeClimbPresentationDuration = 0.65f;

    [SerializeField] private float traversalRecoverySpeed = 12f;

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

    [Header("Focus Mode")] 
    [SerializeField, Min(1f)] private float focusFovReduction = 20f;
    [SerializeField, Min(0.1f)] private float focusFovResponseSpeed = 9f;

    [Header("Landing")]
    [SerializeField] private float landingImpactThreshold = 2.5f;
    [SerializeField] private float landingImpulseScale = 0.09f;
    [SerializeField] private float landingImpactExponent = 1.35f;
    [SerializeField] private float maximumLandingImpulse = 2.2f;
    [SerializeField] private float maximumLandingOffset = 0.14f;
    [SerializeField] private float landingSpringStrength = 160f;
    [SerializeField] private float landingSpringDamping = 20f;

    [SerializeField] private TraversalMotor traversalMotor;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 currentBobOffset;
    private Vector3 currentInertiaOffset;
    private Vector3 previousLocalVelocity;
    private InputAction focusAction;

    private float baseFieldOfView;
    private float bobPhase;
    private float sprintBobWeight;
    private float landingOffset;
    private float landingVelocity;
    private float currentRoll;
    private float currentSlideRoll;
    private float currentSprintPitch;
    private float currentSprintDrop;
    private float traversalTimer;
    private float traversalDuration;
    private float traversalDip;
    private float traversalPitch;
    private float traversalRoll;
    private float vaultRollDirection = 1f;
    private float sprintCameraCycle;
    private float previousSprintAnimationTime;
    private bool sprintCameraCycleInitialized;

    private TraversalType activeTraversalType = TraversalType.None;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        if (playerCamera != null)
            baseFieldOfView = playerCamera.fieldOfView;

        focusAction = GameControls.GetPlayerAction("Focus");
    }

    private void OnEnable()
    {
        playerMovement.Landed += HandleLanding;

        if (traversalMotor != null)
            traversalMotor.TraversalStarted += HandleTraversalStarted;
    }

    private void OnDisable()
    {
        playerMovement.Landed -= HandleLanding;

        if (traversalMotor != null)
            traversalMotor.TraversalStarted -= HandleTraversalStarted;
    }

    private void LateUpdate()
    {
        Vector3 localVelocity =
            playerMovement.transform.InverseTransformDirection(playerMovement.HorizontalVelocity);

        UpdateBob();
        UpdateMovementInertia(localVelocity);
        UpdateStrafeTilt(localVelocity);
        UpdateSprintFov();
        UpdateLanding();
        UpdateSprintPresentation();
        UpdateSlidePresentation();
        UpdateTraversalPresentation();

        transform.localPosition =
            baseLocalPosition +
            currentBobOffset +
            currentInertiaOffset +
            Vector3.up * (landingOffset + currentSprintDrop + traversalDip);

        transform.localRotation =
            baseLocalRotation *
            Quaternion.Euler(
                currentSprintPitch + traversalPitch,
                0f,
                currentRoll + currentSlideRoll + traversalRoll
            );

        previousLocalVelocity = localVelocity;
    }

    private bool TryGetSprintAnimationPhase(out float phase)
    {
        phase = 0f;

        if (viewmodelAnimator == null ||
            !viewmodelAnimator.isActiveAndEnabled)
        {
            return false;
        }

        const int layer = 0;

        AnimatorStateInfo state =
            viewmodelAnimator.GetCurrentAnimatorStateInfo(layer);

        if (viewmodelAnimator.IsInTransition(layer))
        {
            AnimatorStateInfo nextState =
                viewmodelAnimator.GetNextAnimatorStateInfo(layer);

            if (nextState.shortNameHash == SprintStateHash)
                state = nextState;
        }

        if (state.shortNameHash != SprintStateHash)
            return false;

        phase = state.normalizedTime;
        return true;
    }

    private void UpdateBob()
    {
        bool isMoving =
            playerMovement.IsGrounded &&
            playerMovement.HorizontalSpeed > 0.1f &&
            !playerMovement.IsSliding;

        if (!isMoving || !playerMovement.IsSprinting)
            sprintCameraCycleInitialized = false;

        float sprintTarget =
            isMoving && playerMovement.IsSprinting
                ? playerMovement.SprintRatio
                : 0f;

        float sprintBlend =
            1f - Mathf.Exp(-sprintBobBlendSpeed * Time.deltaTime);

        sprintBobWeight = Mathf.Lerp(
            sprintBobWeight,
            sprintTarget,
            sprintBlend
        );

        Vector3 targetOffset = Vector3.zero;

        if (isMoving)
        {
            float frequency = Mathf.Lerp(
                walkBobFrequency,
                sprintBobFrequency,
                playerMovement.SpeedRatio
            );

            bobPhase += frequency * Time.deltaTime;

            float intensity = Mathf.Lerp(
                0.65f,
                1f,
                playerMovement.SpeedRatio
            );

            float amplitudeMultiplier = Mathf.Lerp(
                1f,
                sprintBobAmplitudeMultiplier,
                sprintBobWeight
            );

            // Горизонтальный Bob оставляем прежним.
            float horizontalOffset =
                Mathf.Sin(bobPhase) *
                horizontalBobAmplitude *
                intensity *
                amplitudeMultiplier;

            // Обычная вертикальная раскачка.
            float walkVerticalWave =
                Mathf.Sin(bobPhase * 2f);

            float sprintVerticalWave = walkVerticalWave;

            // Во время Sprint берём фазу непосредственно
            // из анимации рук.
            if (TryGetSprintAnimationPhase(out float animationTime))
            {
                if (!sprintCameraCycleInitialized)
                {
                    sprintCameraCycle = animationTime;
                    sprintCameraCycleInitialized = true;
                }
                else
                {
                    float delta = animationTime - previousSprintAnimationTime;

                    if (delta >= 0f && delta < 0.25f)
                    {
                        sprintCameraCycle +=
                            delta * sprintVerticalBobSpeedMultiplier;
                    }
                    else
                    {
                        sprintCameraCycle = animationTime;
                    }
                }

                previousSprintAnimationTime = animationTime;

                float phase =
                    (sprintCameraCycle + sprintBobPhaseOffset) *
                    Mathf.PI * 4f;

                sprintVerticalWave = Mathf.Sin(phase);
            }
            else
            {
                sprintCameraCycleInitialized = false;
            }

            // Плавно переключаем вертикальный Bob
            // между обычным движением и спринтом.
            float verticalWave = Mathf.Lerp(
                walkVerticalWave,
                sprintVerticalWave,
                sprintBobWeight
            );

            float verticalOffset =
                verticalWave *
                verticalBobAmplitude *
                intensity *
                amplitudeMultiplier;

            targetOffset = new Vector3(
                horizontalOffset,
                verticalOffset,
                0f
            );
        }

        float blend =
            1f - Mathf.Exp(-bobReturnSpeed * Time.deltaTime);

        currentBobOffset = Vector3.Lerp(
            currentBobOffset,
            targetOffset,
            blend
        );
    }

    private void UpdateSlidePresentation()
    {
        float targetRoll = playerMovement.IsSliding
            ? -slideRollAngle
            : 0f;

        float blend = 1f - Mathf.Exp(
            -slideRollResponseSpeed * Time.deltaTime
        );

        currentSlideRoll = Mathf.Lerp(
            currentSlideRoll,
            targetRoll,
            blend
        );
    }

    private void UpdateSprintPresentation()
    {
        float sprintAmount = playerMovement.IsGrounded && playerMovement.IsSprinting ? playerMovement.SprintRatio : 0f;
        float targetPitch = sprintForwardPitch * sprintAmount;
        float targetDrop = -sprintCameraDrop * sprintAmount;
        float blend = 1f - Mathf.Exp(-sprintPresentationSpeed * Time.deltaTime);

        currentSprintPitch = Mathf.Lerp(currentSprintPitch, targetPitch, blend);
        currentSprintDrop = Mathf.Lerp(currentSprintDrop, targetDrop, blend);
    }

    private void HandleTraversalStarted(TraversalType traversalType)
    {
        activeTraversalType = traversalType;
        traversalTimer = 0f;
        traversalDip = 0f;
        traversalPitch = 0f;
        traversalRoll = 0f;

        if (traversalType == TraversalType.Vault)
        {
            vaultRollDirection = -vaultRollDirection;
            traversalDuration = vaultPresentationDuration;
        }
        else if (traversalType == TraversalType.LedgeClimb)
        {
            traversalDuration = ledgeClimbPresentationDuration;
        }
    }

    private void UpdateTraversalPresentation()
    {
        if (activeTraversalType == TraversalType.None)
            return;

        if (playerMovement.MovementMode != PlayerMovementMode.Traversal)
        {
            float recoveryBlend = 1f - Mathf.Exp(-traversalRecoverySpeed * Time.deltaTime);

            traversalDip = Mathf.Lerp(traversalDip, 0f, recoveryBlend);
            traversalPitch = Mathf.Lerp(traversalPitch, 0f, recoveryBlend);
            traversalRoll = Mathf.Lerp(traversalRoll, 0f, recoveryBlend);

            if (Mathf.Abs(traversalDip) < 0.001f &&
                Mathf.Abs(traversalPitch) < 0.01f &&
                Mathf.Abs(traversalRoll) < 0.01f)
            {
                traversalDip = 0f;
                traversalPitch = 0f;
                traversalRoll = 0f;
                activeTraversalType = TraversalType.None;
            }

            return;
        }

        traversalTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(traversalTimer / traversalDuration);
        float impulse = Mathf.Sin(progress * Mathf.PI);

        if (activeTraversalType == TraversalType.Vault)
        {
            traversalDip = -vaultCameraDip * impulse;
            traversalPitch = vaultPitch * impulse;
            traversalRoll = vaultRollAngle * vaultRollDirection * impulse;
        }
        else if (activeTraversalType == TraversalType.LedgeClimb)
        {
            traversalDip = -ledgeClimbCameraDip * impulse;
            traversalPitch = ledgeClimbPitch * impulse;
            traversalRoll = 0f;
        }
    }

    private void UpdateMovementInertia(Vector3 localVelocity)
    {
        if (Time.deltaTime <= 0f)
            return;

        Vector3 localAcceleration = (localVelocity - previousLocalVelocity) / Time.deltaTime;

        float targetX = Mathf.Clamp(
            -localAcceleration.x * inertiaScale,
            -maximumInertiaOffset,
            maximumInertiaOffset
        );

        float targetZ = Mathf.Clamp(
            -localAcceleration.z * inertiaScale,
            -maximumInertiaOffset,
            maximumInertiaOffset
        );

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

        bool canFocus =
            focusAction != null &&
            focusAction.IsPressed() &&
            Cursor.lockState == CursorLockMode.Locked &&
            playerMovement != null &&
            playerMovement.enabled &&
            !playerMovement.IsSprinting &&
            !playerMovement.IsSliding &&
            (traversalMotor == null || !traversalMotor.IsTraversing);

        float sprintAmount =
            playerMovement != null &&
            playerMovement.IsGrounded &&
            playerMovement.IsSprinting
                ? playerMovement.SprintRatio
                : 0f;

        float targetFieldOfView;

        if (canFocus)
        {
            targetFieldOfView = Mathf.Max(
                20f,
                baseFieldOfView - focusFovReduction
            );
        }
        else
        {
            targetFieldOfView =
                baseFieldOfView +
                sprintFovIncrease * sprintAmount;
        }

        float responseSpeed = canFocus
            ? focusFovResponseSpeed
            : fovResponseSpeed;

        float blend =
            1f - Mathf.Exp(-responseSpeed * Time.deltaTime);

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFieldOfView,
            blend
        );
    }

    private void UpdateLanding()
    {
        float springAcceleration =
            -landingOffset * landingSpringStrength -
            landingVelocity * landingSpringDamping;

        landingVelocity += springAcceleration * Time.deltaTime;
        landingOffset += landingVelocity * Time.deltaTime;

        landingOffset = Mathf.Clamp(
            landingOffset,
            -maximumLandingOffset,
            maximumLandingOffset
        );

        if (Mathf.Abs(landingOffset) < 0.0001f &&
            Mathf.Abs(landingVelocity) < 0.001f)
        {
            landingOffset = 0f;
            landingVelocity = 0f;
        }
    }

    private void HandleLanding(float impactSpeed)
    {
        float impact = Mathf.Max(0f, impactSpeed - landingImpactThreshold);

        if (impact <= 0f)
            return;

        float impulse =
            Mathf.Pow(impact, landingImpactExponent) *
            landingImpulseScale;

        impulse = Mathf.Min(impulse, maximumLandingImpulse);

        landingVelocity -= impulse;
    }
}