using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float DirectionThresholdSqr = 0.0001f;
    private const int ClearanceOverlapCapacity = 16;

    [SerializeField] private PlayerMovementProfile movementProfile;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private GroundProbe groundProbe;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;

    private readonly Collider[] clearanceOverlaps = new Collider[ClearanceOverlapCapacity];

    private CharacterController characterController;

    private Vector3 horizontalVelocity;
    private Vector3 slideDirection;

    private float verticalVelocity;
    private float slideTimer;

    private bool isCrouching;
    private bool isSliding;

    private PlayerMovementMode movementMode = PlayerMovementMode.Locomotion;

    private bool HasEnhancedMobility => memberAssembly != null && enhancedMobilityCapability != null && memberAssembly.HasCapability(enhancedMobilityCapability);
    private float CurrentSprintSpeed => HasEnhancedMobility ? movementProfile.EnhancedSprintSpeed : movementProfile.SprintSpeed;
    private float CurrentJumpHeight => HasEnhancedMobility ? movementProfile.EnhancedJumpHeight : movementProfile.JumpHeight;

    public event Action<float> Landed;

    public Vector3 HorizontalVelocity => horizontalVelocity;

    public float VerticalVelocity => verticalVelocity;
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public float SprintRatio => CurrentSprintSpeed > movementProfile.MoveSpeed ? Mathf.InverseLerp(movementProfile.MoveSpeed, CurrentSprintSpeed, HorizontalSpeed) : 0f;
    public float SpeedRatio => CurrentSprintSpeed > 0f ? Mathf.Clamp01(HorizontalSpeed / CurrentSprintSpeed) : 0f;

    public bool IsOnSteepSlope => groundProbe != null && groundProbe.IsGrounded && groundProbe.GroundAngle > movementProfile.SlopeLimit;
    public bool IsGrounded => groundProbe != null && groundProbe.IsGrounded;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching => isCrouching;
    public bool IsSliding => isSliding;
    public PlayerMovementMode MovementMode => movementMode;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ApplyMovementProfile();
    }

    private void Update()
    {
        if (movementProfile == null || inputReader == null || groundProbe == null || characterController == null)
            return;

        if (movementMode != PlayerMovementMode.Locomotion)
            return;

        groundProbe.Probe();

        Vector2 input = inputReader.Move;
        bool groundedBeforeMove = groundProbe.IsGrounded;

        UpdateCrouchAndSlide(groundedBeforeMove);
        UpdateBodyHeight();
        UpdateSprint(input);

        if (isSliding)
            UpdateSlide(input, groundedBeforeMove);
        else
            UpdateHorizontalVelocity(input, groundedBeforeMove);

        UpdateVerticalVelocity(groundedBeforeMove);

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        groundProbe.Probe();

        bool groundedAfterMove = groundProbe.IsGrounded;

        if (!groundedBeforeMove && groundedAfterMove && verticalVelocity < 0f)
            Landed?.Invoke(-verticalVelocity);

        if (groundedAfterMove && !IsOnSteepSlope && verticalVelocity < 0f)
            verticalVelocity = movementProfile.GroundedVelocity;

        UpdateCameraHeight();
    }

    private void ApplyMovementProfile()
    {
        if (movementProfile == null || characterController == null)
            return;

        characterController.height = movementProfile.StandingHeight;
        characterController.radius = movementProfile.Radius;
        characterController.center = new Vector3(0f, movementProfile.StandingHeight * 0.5f, 0f);
        characterController.slopeLimit = movementProfile.SlopeLimit;
        characterController.stepOffset = 0f;
        characterController.skinWidth = movementProfile.SkinWidth;

        if (cameraPivot == null)
            return;

        Vector3 cameraPosition = cameraPivot.localPosition;
        cameraPosition.y = movementProfile.StandingCameraHeight;
        cameraPivot.localPosition = cameraPosition;
    }

    private void UpdateBodyHeight()
    {
        float targetHeight = isCrouching ? movementProfile.CrouchingHeight : movementProfile.StandingHeight;

        characterController.height = Mathf.MoveTowards(characterController.height, targetHeight, movementProfile.CrouchTransitionSpeed * Time.deltaTime);
        characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
    }

    private void UpdateCrouchAndSlide(bool grounded)
    {
        if (isSliding)
        {
            isCrouching = true;
            return;
        }

        if (inputReader.CrouchPressed && CanStartSlide(grounded))
        {
            StartSlide();
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

    private void UpdateSprint(Vector2 input)
    {
        IsSprinting = !isSliding && !isCrouching && inputReader.SprintHeld && input.y > 0f;
    }

    private void UpdateHorizontalVelocity(Vector2 input, bool grounded)
    {
        if (grounded && IsOnSteepSlope)
        {
            UpdateSteepSlopeVelocity();
            return;
        }

        Vector3 direction = transform.forward * input.y + transform.right * input.x * movementProfile.StrafeSpeedMultiplier;
        direction = Vector3.ClampMagnitude(direction, 1f);

        float targetSpeed = isCrouching ? movementProfile.CrouchSpeed : IsSprinting ? CurrentSprintSpeed : movementProfile.MoveSpeed;
        Vector3 targetVelocity = direction * targetSpeed;

        float acceleration;

        if (!grounded)
            acceleration = input.sqrMagnitude > 0f ? movementProfile.AirAcceleration : 0f;
        else if (input.sqrMagnitude <= 0f)
            acceleration = movementProfile.GroundDeceleration;
        else if (IsSprinting && HasEnhancedMobility)
            acceleration = movementProfile.EnhancedSprintAcceleration;
        else
            acceleration = movementProfile.GroundAcceleration;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
    }

    private void UpdateSteepSlopeVelocity()
    {
        Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, groundProbe.GroundNormal).normalized;
        float currentSlideSpeed = Vector3.Dot(horizontalVelocity, slopeDirection);

        if (currentSlideSpeed < movementProfile.SteepSlopeInitialSpeed)
            horizontalVelocity += slopeDirection * (movementProfile.SteepSlopeInitialSpeed - currentSlideSpeed);

        Vector3 targetVelocity = slopeDirection * movementProfile.SteepSlopeMaximumSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, movementProfile.SteepSlopeSlideAcceleration * Time.deltaTime);
    }

    private void UpdateSlide(Vector2 input, bool grounded)
    {
        if (!HasEnhancedMobility || !grounded)
        {
            EndSlide();
            return;
        }

        slideTimer += Time.deltaTime;

        Vector3 desiredDirection = transform.forward * input.y + transform.right * input.x * movementProfile.StrafeSpeedMultiplier;

        if (desiredDirection.sqrMagnitude > DirectionThresholdSqr)
        {
            desiredDirection.Normalize();
            slideDirection = Vector3.RotateTowards(slideDirection, desiredDirection, movementProfile.SlideSteeringSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }

        float currentSpeed = Mathf.MoveTowards(HorizontalSpeed, 0f, movementProfile.SlideDeceleration * Time.deltaTime);
        horizontalVelocity = slideDirection * currentSpeed;

        if (slideTimer >= movementProfile.SlideDuration || currentSpeed <= movementProfile.SlideEndSpeed)
            EndSlide();
    }

    private void UpdateVerticalVelocity(bool grounded)
    {
        if (grounded && !IsOnSteepSlope && verticalVelocity < 0f)
            verticalVelocity = movementProfile.GroundedVelocity;
        else
            verticalVelocity += movementProfile.Gravity * Time.deltaTime;

        if (!grounded || IsOnSteepSlope || isCrouching || !inputReader.JumpPressed)
            return;

        verticalVelocity = Mathf.Sqrt(CurrentJumpHeight * -2f * movementProfile.Gravity);
    }

    private bool CanStartSlide(bool grounded)
    {
        return HasEnhancedMobility && grounded && !IsOnSteepSlope && inputReader.SprintHeld && inputReader.Move.y > 0f && HorizontalSpeed >= movementProfile.SlideMinimumStartSpeed;
    }

    private void StartSlide()
    {
        isSliding = true;
        isCrouching = true;
        IsSprinting = false;
        slideTimer = 0f;

        slideDirection = horizontalVelocity.sqrMagnitude > DirectionThresholdSqr ? horizontalVelocity.normalized : transform.forward;

        float startSpeed = Mathf.Max(HorizontalSpeed, CurrentSprintSpeed) + movementProfile.SlideInitialBoost;
        horizontalVelocity = slideDirection * startSpeed;
    }

    private void EndSlide()
    {
        isSliding = false;
        slideTimer = 0f;
        isCrouching = inputReader.CrouchHeld || !CanStand();
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

    private bool CanStand()
    {
        float radius = Mathf.Max(0f, movementProfile.Radius - movementProfile.SkinWidth);
        float standingHeight = Mathf.Max(movementProfile.StandingHeight, radius * 2f);

        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);

        int overlapCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, clearanceOverlaps, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        for (int index = 0; index < overlapCount; index++)
        {
            Collider overlap = clearanceOverlaps[index];

            if (overlap == null || overlap == characterController)
                continue;

            if (overlap.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
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

        IsSprinting = false;
        isSliding = false;
    }

    public void CompleteTraversal(Vector3 exitVelocity)
    {
        horizontalVelocity = exitVelocity;
        verticalVelocity = movementProfile.GroundedVelocity;
        movementMode = PlayerMovementMode.Locomotion;
    }
}