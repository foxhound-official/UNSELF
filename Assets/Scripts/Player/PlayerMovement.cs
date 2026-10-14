using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 5.5f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float groundAcceleration = 14f;
    [SerializeField] private float groundDeceleration = 18f;
    [SerializeField] private float airAcceleration = 4f;

    [Header("Vertical Movement")]
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Crouch")]
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchingCameraHeight = 0.4f;
    [SerializeField] private float crouchTransitionSpeed = 5f;
    [SerializeField] private Transform cameraPivot;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;
    [SerializeField] private float enhancedSprintSpeed = 6f;
    [SerializeField] private float enhancedSprintAcceleration = 18f;
    [SerializeField] private float enhancedJumpHeight = 1.45f;

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float standingHeight;
    private float standingCameraHeight;
    private bool isCrouching;

    private bool HasEnhancedMobility => memberAssembly != null && enhancedMobilityCapability != null && memberAssembly.HasCapability(enhancedMobilityCapability);
    private float CurrentSprintSpeed => HasEnhancedMobility ? enhancedSprintSpeed : sprintSpeed;
    private float CurrentJumpHeight => HasEnhancedMobility ? enhancedJumpHeight : jumpHeight;

    public event Action<float> Landed;

    public Vector3 HorizontalVelocity => horizontalVelocity;
    public float SprintRatio => CurrentSprintSpeed > moveSpeed ? Mathf.InverseLerp(moveSpeed, CurrentSprintSpeed, HorizontalSpeed) : 0f;
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public float SpeedRatio => CurrentSprintSpeed > 0f ? Mathf.Clamp01(HorizontalSpeed / CurrentSprintSpeed) : 0f;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching => isCrouching;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        standingHeight = characterController.height;
        standingCameraHeight = cameraPivot.localPosition.y;
    }

    private void Update()
    {
        Vector2 input = ReadMovementInput();

        UpdateCrouch();
        UpdateSprint(input);

        bool groundedBeforeMove = characterController.isGrounded;

        UpdateHorizontalVelocity(input, groundedBeforeMove);
        UpdateVerticalVelocity(groundedBeforeMove);

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        bool groundedAfterMove = characterController.isGrounded;

        if (!groundedBeforeMove && groundedAfterMove && verticalVelocity < 0f)
            Landed?.Invoke(-verticalVelocity);

        if (groundedAfterMove && verticalVelocity < 0f)
            verticalVelocity = groundedVelocity;

        UpdateCameraHeight();
    }

    private Vector2 ReadMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current == null)
            return input;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void UpdateHorizontalVelocity(Vector2 input, bool grounded)
    {
        Vector3 direction = transform.forward * input.y + transform.right * input.x;

        float targetSpeed = isCrouching
            ? crouchSpeed
            : IsSprinting
                ? CurrentSprintSpeed
                : moveSpeed;

        Vector3 targetVelocity = direction * targetSpeed;

        float acceleration;

        if (grounded)
        {
            if (input.sqrMagnitude <= 0f)
            {
                acceleration = groundDeceleration;
            }
            else if (IsSprinting && HasEnhancedMobility)
            {
                acceleration = enhancedSprintAcceleration;
            }
            else
            {
                acceleration = groundAcceleration;
            }
        }
        else
        {
            acceleration = input.sqrMagnitude > 0f ? airAcceleration : 0f;
        }

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );
    }

    private void UpdateVerticalVelocity(bool grounded)
    {
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedVelocity;
        else
            verticalVelocity += gravity * Time.deltaTime;

        if (!grounded || isCrouching || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(CurrentJumpHeight * -2f * gravity);
    }

    private void UpdateSprint(Vector2 input)
    {
        IsSprinting =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            input.y > 0f &&
            !isCrouching;
    }

    private void UpdateCrouch()
    {
        bool wantsToCrouch =
            Keyboard.current != null &&
            Keyboard.current.leftCtrlKey.isPressed;

        if (wantsToCrouch)
        {
            isCrouching = true;
        }
        else if (isCrouching && CanStand())
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;

        characterController.height = Mathf.MoveTowards(
            characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        characterController.center = new Vector3(
            0f,
            characterController.height / 2f,
            0f
        );
    }

    private void UpdateCameraHeight()
    {
        float targetHeight = isCrouching
            ? crouchingCameraHeight
            : standingCameraHeight;

        Vector3 position = cameraPivot.localPosition;

        position.y = Mathf.MoveTowards(
            position.y,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        cameraPivot.localPosition = position;
    }

    private bool CanStand()
    {
        float checkDistance = standingHeight - crouchingHeight;
        Vector3 origin = transform.position + Vector3.up * (crouchingHeight + 0.05f);

        return !Physics.Raycast(
            origin,
            Vector3.up,
            checkDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}