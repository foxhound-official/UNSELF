using UnityEngine;

public class PlayerViewmodelAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int SprintingHash = Animator.StringToHash("Sprinting");
    private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
    private static readonly int SlidingHash = Animator.StringToHash("Sliding");

    private static readonly int VaultHash = Animator.StringToHash("Vault");
    private static readonly int LedgeClimbHash = Animator.StringToHash("LedgeClimb");
    private static readonly int LandHash = Animator.StringToHash("Land");
    private static readonly int FireHash = Animator.StringToHash("Fire");

    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TraversalMotor traversalMotor;
    [SerializeField] private PlayerWeapon playerWeapon;

    private void OnEnable()
    {
        if (playerMovement != null)
            playerMovement.Landed += HandleLanding;

        if (traversalMotor != null)
            traversalMotor.TraversalStarted += HandleTraversalStarted;

        if (playerWeapon != null)
            playerWeapon.Fired += HandleFire;
    }

    private void OnDisable()
    {
        if (playerMovement != null)
            playerMovement.Landed -= HandleLanding;

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

        animator.SetBool(GroundedHash, playerMovement.IsGrounded);
        animator.SetBool(SprintingHash, playerMovement.IsSprinting);
        animator.SetBool(CrouchingHash, playerMovement.IsCrouching);
        animator.SetBool(SlidingHash, playerMovement.IsSliding);
    }

    private void HandleLanding(float impactSpeed)
    {
        if (animator == null)
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

        animator.SetTrigger(FireHash);
    }
}