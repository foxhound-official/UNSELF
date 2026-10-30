    using UnityEngine;

public class PlayerViewmodelAnimator : MonoBehaviour
{
    private static readonly int FallAllowedHash = Animator.StringToHash("FallAllowed");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int SprintingHash = Animator.StringToHash("Sprinting");
    private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
    private static readonly int SlidingHash = Animator.StringToHash("Sliding"); 

    private static readonly int VaultHash = Animator.StringToHash("Vault");
    private static readonly int LedgeClimbHash = Animator.StringToHash("LedgeClimb");
    private static readonly int LandHash = Animator.StringToHash("Land");
    private static readonly int JumpStartedHash = Animator.StringToHash("JumpStarted");
    private static readonly int FireHash = Animator.StringToHash("Fire");
    private static readonly int TraversingHash = Animator.StringToHash("Traversing");

    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TraversalMotor traversalMotor;
    [SerializeField] private PlayerWeapon playerWeapon;

    [Header("Traversal Recovery")]
    [SerializeField, Min(0f)] private float fallGraceDuration = 0.15f;
    [SerializeField, Min(0f)] private float realFallSpeedThreshold = 2f;
    [SerializeField, Min(0f)] private float landImpactThreshold = 2.5f;

    [Header("Ground Contact Animation")]
    [SerializeField, Min(0f)] private float groundContactGraceDuration = 0.12f;

    private bool wasTraversing;
    private float fallGraceUntil;
    private float lastPhysicallyGroundedTime = float.NegativeInfinity;
    private float jumpStartedTime;
    private bool jumpInProgress;
    private bool leftGroundSinceJump;

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.Landed += HandleLanding;
            playerMovement.Jumped += HandleJumpStarted;
        }

        if (traversalMotor != null)
            traversalMotor.TraversalStarted += HandleTraversalStarted;

        if (playerWeapon != null)
            playerWeapon.Fired += HandleFire;
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.Landed -= HandleLanding;
            playerMovement.Jumped -= HandleJumpStarted;
        }

        if (traversalMotor != null)
            traversalMotor.TraversalStarted -= HandleTraversalStarted;

        if (playerWeapon != null)
            playerWeapon.Fired -= HandleFire;
    }

    private void Update()
    {
        if (animator == null || playerMovement == null)
            return;

        animator.SetFloat(SpeedHash, playerMovement.SpeedRatio);
        animator.SetFloat(VerticalSpeedHash, playerMovement.VerticalVelocity);

        bool isPhysicallyGrounded = playerMovement.IsGrounded;

        if (isPhysicallyGrounded)
            lastPhysicallyGroundedTime = Time.time;

        // Настоящий прыжок отключает сглаживание контакта с землёй.
        if (jumpInProgress)
        {
            if (!isPhysicallyGrounded)
                leftGroundSinceJump = true;
            else if (leftGroundSinceJump || Time.time - jumpStartedTime > 0.25f)
                jumpInProgress = false;
        }

        // Кратковременная потеря опоры на ступеньках не считается падением.
        bool isGroundedForAnimation = !jumpInProgress && (isPhysicallyGrounded ||
            (Time.time - lastPhysicallyGroundedTime <= groundContactGraceDuration &&
            playerMovement.VerticalVelocity > -realFallSpeedThreshold));

        animator.SetBool(GroundedHash, isGroundedForAnimation);
        animator.SetBool(SprintingHash, playerMovement.IsSprinting);
        animator.SetBool(CrouchingHash, playerMovement.IsCrouching);
        animator.SetBool(SlidingHash, playerMovement.IsSliding);

        bool isTraversing =
            traversalMotor != null && traversalMotor.IsTraversing;

        if (wasTraversing && !isTraversing)
            fallGraceUntil = Time.time + fallGraceDuration;

        wasTraversing = isTraversing;

        bool isBriefLandingGap =
            Time.time < fallGraceUntil &&
            !playerMovement.IsGrounded &&
            playerMovement.VerticalVelocity > -realFallSpeedThreshold;

        animator.SetBool(TraversingHash, isTraversing);

        animator.SetBool(
            FallAllowedHash,
            !isTraversing && !isBriefLandingGap
        );
    }

    private void HandleJumpStarted()
    {
        jumpInProgress = true;
        leftGroundSinceJump = false;
        jumpStartedTime = Time.time;

        if (animator != null)
            animator.SetTrigger(JumpStartedHash);
    }

    private void HandleLanding(float impactSpeed)
    {
        if (animator == null || impactSpeed < landImpactThreshold)
            return;

        if (traversalMotor != null && traversalMotor.IsTraversing)
            return;

        animator.SetTrigger(LandHash);
    }

    private void HandleTraversalStarted(TraversalType traversalType)
    {
        if (animator == null)
            return;

        if (traversalType == TraversalType.Vault)
            animator.SetTrigger(VaultHash);
        else if (traversalType == TraversalType.LedgeClimb)
            animator.SetTrigger(LedgeClimbHash);
    }

    private void HandleFire()
    {
        if (animator == null)
            return;

        if (traversalMotor != null && traversalMotor.IsTraversing)
            return;

        animator.SetTrigger(FireHash);
    }
}