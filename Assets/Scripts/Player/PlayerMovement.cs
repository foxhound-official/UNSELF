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
    private float maximumAirborneFallSpeed;

    private bool sprintHeld;
    private bool jumpRequested;
    private bool slideRequested;
    private bool isCrouching;
    private bool isSliding;
    private bool wasGrounded;

    private PlayerMovementMode movementMode = PlayerMovementMode.Locomotion;

    private bool HasEnhancedMobility =>
        memberAssembly != null &&
        enhancedMobilityCapability != null &&
        memberAssembly.HasCapability(enhancedMobilityCapability);

    private float CurrentSprintSpeed =>
        HasEnhancedMobility ? movementProfile.EnhancedSprintSpeed : movementProfile.SprintSpeed;

    private float CurrentJumpHeight =>
        HasEnhancedMobility ? movementProfile.EnhancedJumpHeight : movementProfile.JumpHeight;

    public event Action<float> Landed;

    public Vector3 HorizontalVelocity => horizontalVelocity;

    public float VerticalVelocity => verticalVelocity;
    public float HorizontalSpeed => horizontalVelocity.magnitude;

    public float SprintRatio =>
        CurrentSprintSpeed > movementProfile.MoveSpeed
            ? Mathf.InverseLerp(movementProfile.MoveSpeed, CurrentSprintSpeed, HorizontalSpeed)
            : 0f;

    public float SpeedRatio =>
        CurrentSprintSpeed > 0f
            ? Mathf.Clamp01(HorizontalSpeed / CurrentSprintSpeed)
            : 0f;

    public bool IsOnSteepSlope =>
        motor != null &&
        motor.GroundingStatus.FoundAnyGround &&
        !motor.GroundingStatus.IsStableOnGround;

    public bool IsGrounded =>
        motor != null &&
        motor.GroundingStatus.IsStableOnGround;

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
        if (traversalMotor != null && traversalMotor.TryStartTraversal())
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

        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;

        jumpRequested = false;
        slideRequested = false;
    }

    private void ApplyMovementProfile()
    {
        if (movementProfile == null || motor == null)
            return;

        motor.SetCapsuleDimensions(
            movementProfile.Radius,
            movementProfile.StandingHeight,
            movementProfile.StandingHeight * 0.5f
        );

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
        float targetHeight = isCrouching
            ? movementProfile.CrouchingHeight
            : movementProfile.StandingHeight;

        float newHeight = Mathf.MoveTowards(
            motor.Capsule.height,
            targetHeight,
            movementProfile.CrouchTransitionSpeed * deltaTime
        );

        // Keeping the offset at half height anchors the capsule at the player's feet.
        motor.SetCapsuleDimensions(
            movementProfile.Radius,
            newHeight,
            newHeight * 0.5f
        );
    }

    private void UpdateCameraHeight()
    {
        if (cameraPivot == null)
            return;

        float targetHeight = isCrouching
            ? movementProfile.CrouchingCameraHeight
            : movementProfile.StandingCameraHeight;

        Vector3 position = cameraPivot.localPosition;

        position.y = Mathf.MoveTowards(
            position.y,
            targetHeight,
            movementProfile.CrouchTransitionSpeed * Time.deltaTime
        );

        cameraPivot.localPosition = position;
    }

    private bool CanStand()
    {
        float radius = Mathf.Max(0.01f, movementProfile.Radius - 0.01f);
        float standingHeight = Mathf.Max(movementProfile.StandingHeight, radius * 2f);

        Vector3 up = motor.CharacterUp;
        Vector3 position = motor.TransientPosition;

        Vector3 bottom = position + up * radius;
        Vector3 top = position + up * (standingHeight - radius);

        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            clearanceOverlaps,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

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
    private bool CanStartSlide(Vector3 currentVelocity)
    {
        if (isSliding)
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
        isSliding = true;
        isCrouching = true;
        IsSprinting = false;
        slideTimer = 0f;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

        Vector3 initialDirection = planarVelocity.sqrMagnitude > DirectionThresholdSqr
            ? planarVelocity.normalized
            : motor.CharacterForward;

        slideDirection = motor.GetDirectionTangentToSurface(
            initialDirection,
            motor.GroundingStatus.GroundNormal
        );

        float startSpeed =
            Mathf.Max(planarVelocity.magnitude, CurrentSprintSpeed) +
            movementProfile.SlideInitialBoost;

        currentVelocity = slideDirection * startSpeed;
    }

    private void UpdateSlide(
        ref Vector3 currentVelocity,
        Vector3 moveDirection,
        float deltaTime)
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
            Vector3 desiredDirection = motor.GetDirectionTangentToSurface(
                moveDirection.normalized,
                groundNormal
            );

            slideDirection = Vector3.RotateTowards(
                slideDirection,
                desiredDirection,
                movementProfile.SlideSteeringSpeed * Mathf.Deg2Rad * deltaTime,
                0f
            );
        }

        slideDirection = motor.GetDirectionTangentToSurface(slideDirection, groundNormal);

        float currentSpeed = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude;

        float newSpeed = Mathf.MoveTowards(
            currentSpeed,
            0f,
            movementProfile.SlideDeceleration * deltaTime
        );

        currentVelocity = slideDirection * newSpeed;

        if (slideTimer >= movementProfile.SlideDuration ||
            newSpeed <= movementProfile.SlideEndSpeed)
        {
            Debug.Log(
                $"[SLIDE END] Frame={Time.frameCount} " +
                $"Timer={slideTimer:F2}/{movementProfile.SlideDuration:F2} " +
                $"Speed={newSpeed:F2}/{movementProfile.SlideEndSpeed:F2} " +
                $"CtrlHeld={inputReader.CrouchHeld}"
            );

            EndSlide();
        }
    }

    private void EndSlide()
    {
        isSliding = false;
        slideTimer = 0f;

        isCrouching = inputReader.CrouchHeld || !CanStand();
    }

    private void UpdateGroundVelocity(
        ref Vector3 currentVelocity,
        Vector3 moveDirection,
        float targetSpeed,
        float deltaTime)
    {
        Vector3 groundNormal = motor.GroundingStatus.GroundNormal;

        currentVelocity =
            motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) *
            currentVelocity.magnitude;

        Vector3 targetVelocity = Vector3.zero;

        if (moveDirection.sqrMagnitude > DirectionThresholdSqr)
        {
            Vector3 groundMoveDirection =
                motor.GetDirectionTangentToSurface(moveDirection, groundNormal);

            targetVelocity = groundMoveDirection * targetSpeed;
        }

        float acceleration;

        if (moveDirection.sqrMagnitude <= DirectionThresholdSqr)
            acceleration = movementProfile.GroundDeceleration;
        else if (IsSprinting && HasEnhancedMobility)
            acceleration = movementProfile.EnhancedSprintAcceleration;
        else
            acceleration = movementProfile.GroundAcceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            acceleration * deltaTime
        );
    }

    private void UpdateAirVelocity(
        ref Vector3 currentVelocity,
        Vector3 moveDirection,
        float targetSpeed,
        float deltaTime)
    {
        Vector3 up = motor.CharacterUp;

        float verticalSpeed = Vector3.Dot(currentVelocity, up);
        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, up);
        Vector3 targetPlanarVelocity = moveDirection * targetSpeed;

        if (moveDirection.sqrMagnitude > DirectionThresholdSqr)
        {
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                targetPlanarVelocity,
                movementProfile.AirAcceleration * deltaTime
            );
        }

        verticalSpeed += movementProfile.Gravity * deltaTime;

        currentVelocity = planarVelocity + up * verticalSpeed;
    }

    private void TryJump(ref Vector3 currentVelocity)
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (isCrouching || isSliding)
            return;

        float jumpSpeed = Mathf.Sqrt(
            CurrentJumpHeight * -2f * movementProfile.Gravity
        );

        Vector3 up = motor.CharacterUp;
        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, up);

        currentVelocity = planarVelocity + up * jumpSpeed;

        motor.ForceUnground();
    }

    private void SyncVelocity(Vector3 velocity)
    {
        if (motor == null)
            return;

        verticalVelocity = Vector3.Dot(velocity, motor.CharacterUp);
        horizontalVelocity = Vector3.ProjectOnPlane(velocity, motor.CharacterUp);
    }

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

        maximumAirborneFallSpeed = 0f;

        jumpRequested = false;
        slideRequested = false;

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

    public void CompleteTraversal(Vector3 exitVelocity)
    {
        movementMode = PlayerMovementMode.Locomotion;

        maximumAirborneFallSpeed = 0f;
        jumpRequested = false;
        slideRequested = false;

        motor.BaseVelocity = exitVelocity;
        SyncVelocity(exitVelocity);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        if (!isActiveAndEnabled ||
            movementProfile == null ||
            motor == null ||
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
        if (!isActiveAndEnabled ||
            movementProfile == null ||
            motor == null ||
            movementMode != PlayerMovementMode.Locomotion)
        {
            currentVelocity = Vector3.zero;
            SyncVelocity(currentVelocity);
            return;
        }

        Vector3 moveDirection =
            motor.CharacterForward * moveInput.y +
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
            UpdateSlide(ref currentVelocity, moveDirection, deltaTime);
            SyncVelocity(currentVelocity);
            return;
        }

        float targetSpeed = isCrouching
            ? movementProfile.CrouchSpeed
            : IsSprinting
                ? CurrentSprintSpeed
                : movementProfile.MoveSpeed;

        if (motor.GroundingStatus.IsStableOnGround)
        {
            UpdateGroundVelocity(ref currentVelocity, moveDirection, targetSpeed, deltaTime);
            TryJump(ref currentVelocity);
        }
        else
        {
            UpdateAirVelocity(ref currentVelocity, moveDirection, targetSpeed, deltaTime);

            float fallSpeed = Mathf.Max(
                0f,
                -Vector3.Dot(currentVelocity, motor.CharacterUp)
            );

            maximumAirborneFallSpeed = Mathf.Max(maximumAirborneFallSpeed, fallSpeed);
            jumpRequested = false;
        }

        SyncVelocity(currentVelocity);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        bool grounded = motor.GroundingStatus.IsStableOnGround;

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

    public void OnGroundHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}