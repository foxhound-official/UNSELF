using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementProfile", menuName = "UNSELF/Player/Movement Profile")]
public class PlayerMovementProfile : ScriptableObject
{
    [Header("Body")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchingHeight = 1.1f;
    [SerializeField] private float radius = 0.35f;
    [SerializeField] private float slopeLimit = 45f;
    [SerializeField] private float stepOffset = 0.3f;

    [Header("Camera")]
    [SerializeField] private float standingCameraHeight = 1.62f;
    [SerializeField] private float crouchingCameraHeight = 0.95f;
    [SerializeField] private float crouchTransitionSpeed = 5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 2.2f;
    [SerializeField] private float strafeSpeedMultiplier = 0.9f;
    [SerializeField] private float groundAcceleration = 24f;
    [SerializeField] private float groundDeceleration = 28f;
    [SerializeField] private float airAcceleration = 7f;

    [Header("Vertical Movement")]
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Enhanced Mobility")]
    [SerializeField] private float enhancedSprintSpeed = 6.5f;
    [SerializeField] private float enhancedSprintAcceleration = 28f;
    [SerializeField] private float enhancedJumpHeight = 1.45f;

    [Header("Slide")]
    [SerializeField] private float slideMinimumStartSpeed = 4.5f;
    [SerializeField] private float slideInitialBoost = 0.8f;
    [SerializeField] private float slideDeceleration = 3f;
    [SerializeField] private float slideEndSpeed = 2.5f;
    [SerializeField] private float slideDuration = 1.15f;
    [SerializeField] private float slideSteeringSpeed = 35f;
    [SerializeField, Min(0f)] private float slideCooldown = 0.9f;
    [SerializeField, Min(0f)] private float slideJumpSpeedBoost = 2.2f;
    [SerializeField, Min(0f)] private float slideJumpMinimumSpeed = 9f;

    [Header("Steep Slopes")]
    [SerializeField] private float steepSlopeSlideAcceleration = 12f;
    [SerializeField] private float steepSlopeMaximumSpeed = 6f;
    [SerializeField] private float steepSlopeInitialSpeed = 2f;

    public float StandingHeight => standingHeight;
    public float CrouchingHeight => crouchingHeight;
    public float Radius => radius;
    public float SlopeLimit => slopeLimit;
    public float StepOffset => stepOffset;

    public float StandingCameraHeight => standingCameraHeight;
    public float CrouchingCameraHeight => crouchingCameraHeight;
    public float CrouchTransitionSpeed => crouchTransitionSpeed;

    public float MoveSpeed => moveSpeed;
    public float SprintSpeed => sprintSpeed;
    public float CrouchSpeed => crouchSpeed;
    public float StrafeSpeedMultiplier => strafeSpeedMultiplier;
    public float GroundAcceleration => groundAcceleration;
    public float GroundDeceleration => groundDeceleration;
    public float AirAcceleration => airAcceleration;

    public float Gravity => gravity;
    public float JumpHeight => jumpHeight;
    public float GroundedVelocity => groundedVelocity;

    public float EnhancedSprintSpeed => enhancedSprintSpeed;
    public float EnhancedSprintAcceleration => enhancedSprintAcceleration;
    public float EnhancedJumpHeight => enhancedJumpHeight;

    public float SlideMinimumStartSpeed => slideMinimumStartSpeed;
    public float SlideInitialBoost => slideInitialBoost;
    public float SlideDeceleration => slideDeceleration;
    public float SlideEndSpeed => slideEndSpeed;
    public float SlideDuration => slideDuration;
    public float SlideSteeringSpeed => slideSteeringSpeed;
    public float SlideCooldown => slideCooldown;
    public float SlideJumpSpeedBoost => slideJumpSpeedBoost;
    public float SlideJumpMinimumSpeed => slideJumpMinimumSpeed;

    public float SteepSlopeSlideAcceleration => steepSlopeSlideAcceleration;
    public float SteepSlopeMaximumSpeed => steepSlopeMaximumSpeed;
    public float SteepSlopeInitialSpeed => steepSlopeInitialSpeed;
}