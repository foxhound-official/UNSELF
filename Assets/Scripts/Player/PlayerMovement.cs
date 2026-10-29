using System;
using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerMovement : MonoBehaviour, ICharacterController
{
    private const float DirectionThresholdSqr = 0.0001f;
    private const int ClearanceOverlapCapacity = 16;

    [SerializeField] private PlayerMovementProfile movementProfile;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Transform cameraPivot;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;

    private readonly Collider[] clearanceOverlaps = new Collider[ClearanceOverlapCapacity];

    private KinematicCharacterMotor motor;
    private PlayerLook playerLook;
    private TraversalMotor traversalMotor;

    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private Vector3 slideDirection;

    private float verticalVelocity;
    private float slideTimer;
    private float nextSlideAllowedTime;
    private float maximumAirborneFallSpeed;

    [Header("Vault Exit Momentum")]
    [SerializeField, Min(0.01f)] private float vaultBoostRiseTime = 0.18f;
    [SerializeField, Min(0.01f)] private float vaultBoostFallTime = 0.35f;

    private bool vaultBoostActive;
    private float vaultBoostElapsed;
    private float vaultBoostEntrySpeed;
    private float vaultBoostAmount;
    private Vector3 vaultBoostDirection;

    [Header("Jump Assistance")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;

    private float lastStableGroundedTime = float.NegativeInfinity;

    private bool sprintHeld;
    private bool jumpRequested;
    private bool slideRequested;
    private bool isCrouching;
    private bool isSliding;
    private bool wasGrounded;

    private PlayerMovementMode movementMode = PlayerMovementMode.Locomotion;

    // Наличие MEMBER ловкости определяет доступность подката и усиленные параметры передвижения.
    private bool HasEnhancedMobility => memberAssembly != null && enhancedMobilityCapability != null &&
        memberAssembly.HasCapability(enhancedMobilityCapability);

    private float CurrentSprintSpeed =>
        HasEnhancedMobility ? movementProfile.EnhancedSprintSpeed : movementProfile.SprintSpeed;

    private float CurrentJumpHeight =>
        HasEnhancedMobility ? movementProfile.EnhancedJumpHeight : movementProfile.JumpHeight;

    public event Action<float> Landed;

    public Vector3 HorizontalVelocity => horizontalVelocity;

    public float VerticalVelocity => verticalVelocity;
    public float HorizontalSpeed => horizontalVelocity.magnitude;

    public float SprintRatio => CurrentSprintSpeed > movementProfile.MoveSpeed
        ? Mathf.InverseLerp(movementProfile.MoveSpeed, CurrentSprintSpeed, HorizontalSpeed)
        : 0f;

    public float SpeedRatio => CurrentSprintSpeed > 0f ? Mathf.Clamp01(HorizontalSpeed / CurrentSprintSpeed) : 0f;

    public bool IsOnSteepSlope => motor != null && motor.GroundingStatus.FoundAnyGround &&
        !motor.GroundingStatus.IsStableOnGround;

    public bool IsGrounded => motor != null && motor.GroundingStatus.IsStableOnGround;

    public bool IsSprinting { get; private set; }
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;
    public PlayerMovementMode MovementMode => movementMode;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        playerLook = GetComponent<PlayerLook>();
        traversalMotor = GetComponent<TraversalMotor>();

        motor.CharacterController = this;
    }

    private void Start()
    {
        ApplyMovementProfile();
        wasGrounded = motor.GroundingStatus.IsStableOnGround;
    }

    private void Update()
    {
        if (movementProfile == null || inputReader == null || motor == null)
            return;

        if (movementMode != PlayerMovementMode.Locomotion)
            return;

        moveInput = inputReader.Move;
        sprintHeld = inputReader.SprintHeld;

        if (inputReader.CrouchPressed)
            slideRequested = true;

        UpdateCrouchState();
        UpdateCameraHeight();

        IsSprinting = !isSliding && !isCrouching && sprintHeld && moveInput.y > 0f;

        if (!inputReader.JumpPressed)
            return;

        // Traversal has priority over a regular jump when both use the same input.
       if (!isSliding && traversalMotor != null && traversalMotor.TryStartTraversal())
        {
            jumpRequested = false;
            return;
        }

        jumpRequested = true;
    }

    private void OnDisable()
    {
        if (motor != null)
            motor.BaseVelocity = Vector3.zero;

        vaultBoostActive = false;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        jumpRequested = false;
        slideRequested = false;
    }

    private void ApplyMovementProfile()
    {
        if (movementProfile == null || motor == null)
            return;

        motor.SetCapsuleDimensions(movementProfile.Radius, movementProfile.StandingHeight,
            movementProfile.StandingHeight * 0.5f);

        motor.MaxStableSlopeAngle = movementProfile.SlopeLimit;
        motor.MaxStepHeight = movementProfile.StepOffset;

        if (cameraPivot == null)
            return;

        Vector3 cameraPosition = cameraPivot.localPosition;
        cameraPosition.y = movementProfile.StandingCameraHeight;
        cameraPivot.localPosition = cameraPosition;
    }

    private void UpdateCrouchState()
    {
        if (isSliding)
        {
            isCrouching = true;
            return;
        }

        if (inputReader.CrouchHeld)
        {
            isCrouching = true;
            return;
        }

        if (isCrouching && CanStand())
            isCrouching = false;
    }

    private void UpdateBodyHeight(float deltaTime)
    {
        float targetHeight = isCrouching ? movementProfile.CrouchingHeight : movementProfile.StandingHeight;

        float newHeight = Mathf.MoveTowards(motor.Capsule.height, targetHeight,
            movementProfile.CrouchTransitionSpeed * deltaTime);

        // Центр капсулы всегда на половине высоты, поэтому её низ остаётся на уровне ног.
        motor.SetCapsuleDimensions(movementProfile.Radius, newHeight, newHeight * 0.5f);
    }

    private void UpdateCameraHeight()
    {
        if (cameraPivot == null)
            return;

        float targetHeight = isCrouching ? movementProfile.CrouchingCameraHeight : movementProfile.StandingCameraHeight;

        Vector3 position = cameraPivot.localPosition;

        position.y = Mathf.MoveTowards(position.y, targetHeight, movementProfile.CrouchTransitionSpeed * Time.deltaTime);

        cameraPivot.localPosition = position;
    }

    // Проверяем свободное место для выхода из приседа и прыжка из подката.
    private bool CanStand()
    {
        float radius = Mathf.Max(0.01f, movementProfile.Radius - 0.01f);
        float standingHeight = Mathf.Max(movementProfile.StandingHeight, radius * 2f);

        Vector3 up = motor.CharacterUp;
        Vector3 position = motor.TransientPosition;

        Vector3 bottom = position + up * radius;
        Vector3 top = position + up * (standingHeight - radius);

        int overlapCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, clearanceOverlaps, Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        for (int index = 0; index < overlapCount; index++)
        {
            Collider overlap = clearanceOverlaps[index];

            if (overlap == null || overlap == motor.Capsule)
                continue;

            if (overlap.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    // Подкат требует MEMBER ловкости, устойчивой опоры, спринта и достаточной скорости.
    private bool CanStartSlide(Vector3 currentVelocity)
    {
        if (isSliding)
            return false;

        // Следующий подкат ещё на кулдауне.
        if (Time.time < nextSlideAllowedTime)
            return false;

        if (!HasEnhancedMobility)
            return false;

        if (!motor.GroundingStatus.IsStableOnGround)
            return false;

        if (!sprintHeld || moveInput.y <= 0f)
            return false;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

        return planarVelocity.magnitude >= movementProfile.SlideMinimumStartSpeed;
    }

    private void StartSlide(ref Vector3 currentVelocity)
    {
        // Подкат заменяет ускорение после Vault собственной моделью движения.
        vaultBoostActive = false;
        isSliding = true;
        isCrouching = true;
        IsSprinting = false;
        slideTimer = 0f;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

        Vector3 initialDirection = planarVelocity.sqrMagnitude > DirectionThresholdSqr ? planarVelocity.normalized
            : motor.CharacterForward;

        slideDirection = motor.GetDirectionTangentToSurface(initialDirection, motor.GroundingStatus.GroundNormal);

        float startSpeed = Mathf.Max(planarVelocity.magnitude, CurrentSprintSpeed) + movementProfile.SlideInitialBoost;

        currentVelocity = slideDirection * startSpeed;
    }

    // При подкате скорость постепенно падает, но направление можно корректировать.
    private void UpdateSlide(ref Vector3 currentVelocity, Vector3 moveDirection, float deltaTime)
    {
        if (!HasEnhancedMobility || !motor.GroundingStatus.IsStableOnGround)
        {
            EndSlide();
            return;
        }

        slideTimer += deltaTime;

        Vector3 groundNormal = motor.GroundingStatus.GroundNormal;

        if (moveDirection.sqrMagnitude > DirectionThresholdSqr)
        {
            Vector3 desiredDirection = motor.GetDirectionTangentToSurface(moveDirection.normalized, groundNormal);

            slideDirection = Vector3.RotateTowards(slideDirection, desiredDirection,
                movementProfile.SlideSteeringSpeed * Mathf.Deg2Rad * deltaTime, 0f);
        }

        slideDirection = motor.GetDirectionTangentToSurface(slideDirection, groundNormal);

        float currentSpeed = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude;

        float newSpeed = Mathf.MoveTowards(currentSpeed, 0f, movementProfile.SlideDeceleration * deltaTime);

        currentVelocity = slideDirection * newSpeed;

        if (slideTimer >= movementProfile.SlideDuration || newSpeed <= movementProfile.SlideEndSpeed)
        {
            EndSlide();
        }
    }

    private void EndSlide()
    {
        if (!isSliding)
            return;

        isSliding = false;
        slideTimer = 0f;

        // Начинаем отсчёт после завершения подката.
        nextSlideAllowedTime = Time.time + movementProfile.SlideCooldown;

        isCrouching = inputReader.CrouchHeld || !CanStand();
    }

    // На земле скорость плавно приближается к целевой; без ввода действует торможение.
    private void UpdateGroundVelocity(ref Vector3 currentVelocity, Vector3 moveDirection, float targetSpeed,
        float deltaTime)
    {
        Vector3 groundNormal = motor.GroundingStatus.GroundNormal;

        currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * currentVelocity.magnitude;

        Vector3 targetVelocity = Vector3.zero;

        if (moveDirection.sqrMagnitude > DirectionThresholdSqr)
        {
            Vector3 groundMoveDirection = motor.GetDirectionTangentToSurface(moveDirection, groundNormal);

            targetVelocity = groundMoveDirection * targetSpeed;
        }

        float acceleration;

        if (moveDirection.sqrMagnitude <= DirectionThresholdSqr)
            acceleration = movementProfile.GroundDeceleration;
        else if (IsSprinting && HasEnhancedMobility)
            acceleration = movementProfile.EnhancedSprintAcceleration;
        else
            acceleration = movementProfile.GroundAcceleration;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * deltaTime);
    }

    // В воздухе сохраняем полученный импульс и разрешаем ограниченную коррекцию направления.
    private void UpdateAirVelocity(ref Vector3 currentVelocity, Vector3 moveDirection, float targetSpeed, float deltaTime)
    {
        Vector3 up = motor.CharacterUp;

        float verticalSpeed = Vector3.Dot(currentVelocity, up);
        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, up);
        // Не срезаем ускорение от Slide Jump до обычной скорости спринта.
        float desiredSpeed = Mathf.Max(targetSpeed, planarVelocity.magnitude);

        Vector3 targetPlanarVelocity = moveDirection * desiredSpeed;

        if (moveDirection.sqrMagnitude > DirectionThresholdSqr)
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity,
                movementProfile.AirAcceleration * deltaTime);
        }

        verticalSpeed += movementProfile.Gravity * deltaTime;

        currentVelocity = planarVelocity + up * verticalSpeed;
    }

    // Обычный прыжок и Coyote Time используют одну проверку допустимой опоры.
    private void TryJump(ref Vector3 currentVelocity)
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (isCrouching || isSliding)
            return;

        if (IsOnSteepSlope)
            return;

        bool isGrounded = motor.GroundingStatus.IsStableOnGround;

        bool canCoyoteJump = Time.time - lastStableGroundedTime <= coyoteTime;

        if (!isGrounded && !canCoyoteJump)
            return;

        float jumpSpeed = Mathf.Sqrt(CurrentJumpHeight * -2f * movementProfile.Gravity);

        Vector3 up = motor.CharacterUp;
        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, up);

        currentVelocity = planarVelocity + up * jumpSpeed;

        // Запрещаем повторно использовать тот же допуск.
        lastStableGroundedTime = float.NegativeInfinity;

        motor.ForceUnground();
    }

    private void SyncVelocity(Vector3 velocity)
    {
        if (motor == null)
            return;

        verticalVelocity = Vector3.Dot(velocity, motor.CharacterUp);
        horizontalVelocity = Vector3.ProjectOnPlane(velocity, motor.CharacterUp);
    }

    // Во время Vault и Ledge Climb управление скоростью передаётся TraversalMotor.
    public void SetMovementMode(PlayerMovementMode mode)
    {
        if (movementMode == mode)
            return;

        movementMode = mode;

        if (movementMode != PlayerMovementMode.Locomotion)
            ResetLocomotion();
    }

    private void ResetLocomotion()
    {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        slideDirection = Vector3.zero;
        slideTimer = 0f;

        if (isSliding && movementProfile != null)
        {
            nextSlideAllowedTime = Time.time + movementProfile.SlideCooldown;
        }

        maximumAirborneFallSpeed = 0f;
        lastStableGroundedTime = float.NegativeInfinity;

        jumpRequested = false;
        slideRequested = false;
        vaultBoostActive = false;

        IsSprinting = false;
        isSliding = false;

        if (motor != null)
        {
            motor.BaseVelocity = Vector3.zero;
            wasGrounded = motor.GroundingStatus.IsStableOnGround;
        }
        else
        {
            wasGrounded = false;
        }
    }

    // Сохраняем обычную скорость после Vault. Сам бонус добавляется плавно через целевую скорость.
    public void BeginVaultExitBoost(Vector3 direction, float entrySpeed, float boostAmount)
    {
        vaultBoostDirection = Vector3.ProjectOnPlane(direction, motor.CharacterUp).normalized;
        vaultBoostActive = boostAmount > 0f && vaultBoostDirection.sqrMagnitude > DirectionThresholdSqr;
        vaultBoostElapsed = 0f;
        vaultBoostEntrySpeed = Mathf.Max(0f, entrySpeed);
        vaultBoostAmount = Mathf.Max(0f, boostAmount);
    }

    // Плавно поднимаем и опускаем целевую скорость, не телепортируя персонажа и не меняя BaseVelocity напрямую.
    private float UpdateVaultExitBoost(Vector3 moveDirection, float deltaTime)
    {
        if (!vaultBoostActive)
            return 0f;

        // При остановке, развороте или приседании бонус прекращается.
        if (isCrouching || moveDirection.sqrMagnitude <= DirectionThresholdSqr ||
            Vector3.Dot(moveDirection.normalized, vaultBoostDirection) < 0.5f)
        {
            vaultBoostActive = false;
            return 0f;
        }

        vaultBoostElapsed += deltaTime;

        float rise = Mathf.Max(0.01f, vaultBoostRiseTime);
        float fall = Mathf.Max(0.01f, vaultBoostFallTime);

        if (vaultBoostElapsed >= rise + fall)
        {
            vaultBoostActive = false;
            return 0f;
        }

        float intensity = vaultBoostElapsed < rise
            ? Mathf.SmoothStep(0f, 1f, vaultBoostElapsed / rise)
            : 1f - Mathf.SmoothStep(0f, 1f, (vaultBoostElapsed - rise) / fall);

        return vaultBoostEntrySpeed + vaultBoostAmount * intensity;
    }

    // TraversalMotor возвращает управление вместе со скоростью выхода из манёвра.
    public void CompleteTraversal(Vector3 exitVelocity)
    {
        movementMode = PlayerMovementMode.Locomotion;

        maximumAirborneFallSpeed = 0f;
        lastStableGroundedTime = float.NegativeInfinity;
        jumpRequested = false;
        slideRequested = false;

        motor.BaseVelocity = exitVelocity;
        SyncVelocity(exitVelocity);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        if (!isActiveAndEnabled || movementProfile == null || motor == null ||
            movementMode != PlayerMovementMode.Locomotion)
        {
            return;
        }

        UpdateBodyHeight(deltaTime);
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (playerLook == null)
            return;

        currentRotation = playerLook.DesiredBodyRotation;
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (!isActiveAndEnabled || movementProfile == null || motor == null ||
            movementMode != PlayerMovementMode.Locomotion)
        {
            currentVelocity = Vector3.zero;
            SyncVelocity(currentVelocity);
            return;
        }

        Vector3 moveDirection = motor.CharacterForward * moveInput.y +
            motor.CharacterRight * moveInput.x * movementProfile.StrafeSpeedMultiplier;

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        if (slideRequested)
        {
            if (CanStartSlide(currentVelocity))
                StartSlide(ref currentVelocity);

            slideRequested = false;
        }

        if (isSliding)
        {
            // Slide Jump добавляет горизонтальный импульс, но только если есть место выпрямиться.
            if (jumpRequested)
            {
                if (motor.GroundingStatus.IsStableOnGround && CanStand())
                {
                    Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

                    float slideJumpSpeed = Mathf.Max(planarVelocity.magnitude + movementProfile.SlideJumpSpeedBoost,
                        movementProfile.SlideJumpMinimumSpeed);

                    Vector3 jumpDirection = planarVelocity.sqrMagnitude > DirectionThresholdSqr ? planarVelocity.normalized
                            : slideDirection.normalized;

                    EndSlide();
                    isCrouching = false;

                    // Дополнительный горизонтальный импульс Slide Jump.
                    currentVelocity = jumpDirection * slideJumpSpeed;

                    TryJump(ref currentVelocity);

                    SyncVelocity(currentVelocity);
                    return;
                }

                // Нет устойчивой опоры или места для выпрямления.
                jumpRequested = false;
            }

            // Отпустили Ctrl: прекращаем подкат.
            if (!inputReader.CrouchHeld)
                EndSlide();

            // Если подкат не был прерван, продолжаем его.
            if (isSliding)
            {
                UpdateSlide(ref currentVelocity, moveDirection, deltaTime);
                SyncVelocity(currentVelocity);
                return;
            }
        }

        float targetSpeed = isCrouching
            ? movementProfile.CrouchSpeed
            : IsSprinting ? CurrentSprintSpeed : movementProfile.MoveSpeed;

        // Применяем буст через обычное ускорение, а не мгновенным скачком скорости на выходе из Vault.
        targetSpeed = Mathf.Max(targetSpeed, UpdateVaultExitBoost(moveDirection, deltaTime));

        if (motor.GroundingStatus.IsStableOnGround)
        {
            UpdateGroundVelocity(ref currentVelocity, moveDirection, targetSpeed, deltaTime);
        }
        else
        {
            UpdateAirVelocity(ref currentVelocity, moveDirection, targetSpeed, deltaTime);

            float fallSpeed = Mathf.Max(0f, -Vector3.Dot(currentVelocity, motor.CharacterUp));

            maximumAirborneFallSpeed = Mathf.Max(maximumAirborneFallSpeed, fallSpeed);
        }

        // Coyote Time позволяет прыгнуть в течение небольшого окна после схода с края.
        TryJump(ref currentVelocity);

        SyncVelocity(currentVelocity);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    // Запоминаем последнее приземление для Coyote Time и отдаём событие силы удара камере.
    public void AfterCharacterUpdate(float deltaTime)
    {
        bool grounded = motor.GroundingStatus.IsStableOnGround;
        if (grounded && movementMode == PlayerMovementMode.Locomotion)
        {
            lastStableGroundedTime = Time.time;
        }

        if (!wasGrounded && grounded)
        {
            float impactSpeed = maximumAirborneFallSpeed;
            maximumAirborneFallSpeed = 0f;

            if (impactSpeed > 0f)
                Landed?.Invoke(impactSpeed);
        }

        if (grounded)
            maximumAirborneFallSpeed = 0f;

        wasGrounded = grounded;

        SyncVelocity(motor.BaseVelocity);
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

}
