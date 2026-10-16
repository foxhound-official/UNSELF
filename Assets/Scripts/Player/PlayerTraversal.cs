using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTraversal : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerCameraMotion playerCameraMotion;
    [SerializeField] private MemberAssembly memberAssembly;

    [Header("Climb")]
    [SerializeField] private float moveToHangDuration = 0.3f;
    [SerializeField] private float moveToPullUpDuration = 0.15f;
    [SerializeField] private float pullUpDuration = 0.8f;

    [Header("Shimmy")]
    [SerializeField] private float shimmySpeed = 1.5f;

    private CharacterController characterController;
    private ClimbZone currentClimbZone;
    private PullUpPoint currentPullUpPoint;

    private bool isTraversing;
    private float traversalTime;

    private Vector3 traversalStartPosition;
    private Vector3 traversalTargetPosition;

    private Quaternion traversalStartRotation;
    private Quaternion traversalTargetRotation;

    private TraversalPhase traversalPhase;

    private enum TraversalPhase
    {
        None,
        MoveToHang,
        Hang,
        MoveToPullUp,
        PullUp
    }

    public bool IsTraversing => isTraversing;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isTraversing)
        {
            UpdateTraversal();
            return;
        }

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (currentClimbZone == null)
            return;

        TryStartClimb();
    }

    private void TryStartClimb()
    {
        if (!CanUseClimbZone(currentClimbZone))
            return;

        isTraversing = true;
        currentPullUpPoint = null;

        playerMovement.enabled = false;

        if (playerCameraMotion != null)
            playerCameraMotion.SetSuppressed(true);

        if (playerLook != null)
            playerLook.enabled = false;

        characterController.enabled = false;

        traversalPhase = TraversalPhase.MoveToHang;
        traversalTime = 0f;

        traversalStartPosition = transform.position;
        traversalTargetPosition = currentClimbZone.EntryHangPoint.position;

        traversalStartRotation = transform.rotation;
        traversalTargetRotation = GetHangRotation(currentClimbZone);
    }

    private bool CanUseClimbZone(ClimbZone climbZone)
    {
        if (climbZone == null)
            return false;

        if (climbZone.EntryHangPoint == null)
            return false;

        if (climbZone.ShimmyStart == null || climbZone.ShimmyEnd == null)
            return false;

        if (!IsApproachingClimbZone(climbZone))
            return false;

        MemberCapability requiredCapability = climbZone.RequiredCapability;

        if (requiredCapability == null)
            return true;

        if (memberAssembly == null)
            return false;

        return memberAssembly.HasCapability(requiredCapability);
    }

    private bool IsApproachingClimbZone(ClimbZone climbZone)
    {
        Vector3 toZone =
            climbZone.transform.position - transform.position;

        toZone.y = 0f;

        if (toZone.sqrMagnitude <= 0.001f)
            return true;

        Vector3 approachDirection = climbZone.ApproachDirection;
        approachDirection.y = 0f;

        if (approachDirection.sqrMagnitude <= 0.001f)
            return true;

        toZone.Normalize();
        approachDirection.Normalize();

        float approachDot = Vector3.Dot(
            toZone,
            approachDirection
        );

        return approachDot >= climbZone.MinimumApproachDot;
    }

    private bool IsMovingTowardClimbZone(ClimbZone climbZone)
    {
        Vector3 velocity = playerMovement.HorizontalVelocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude <= 0.001f)
            return false;

        Vector3 approachDirection = climbZone.ApproachDirection;
        approachDirection.y = 0f;

        if (approachDirection.sqrMagnitude <= 0.001f)
            return false;

        velocity.Normalize();
        approachDirection.Normalize();

        float velocityDot = Vector3.Dot(
            velocity,
            approachDirection
        );

        return velocityDot >= climbZone.MinimumVelocityDot;
    }

    private Quaternion GetHangRotation(ClimbZone climbZone)
    {
        Vector3 direction = climbZone.ApproachDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );
    }

    private void UpdateTraversal()
    {
        switch (traversalPhase)
        {
            case TraversalPhase.MoveToHang:
                UpdateMoveToHang();
                break;

            case TraversalPhase.Hang:
                UpdateHang();
                break;

            case TraversalPhase.MoveToPullUp:
                UpdateMoveToPullUp();
                break;

            case TraversalPhase.PullUp:
                UpdatePullUp();
                break;
        }
    }

    private void UpdateMoveToHang()
    {
        traversalTime += Time.deltaTime;

        float progress = GetProgress(
            traversalTime,
            moveToHangDuration
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        transform.position = Vector3.Lerp(
            traversalStartPosition,
            traversalTargetPosition,
            smoothProgress
        );

        transform.rotation = Quaternion.Slerp(
            traversalStartRotation,
            traversalTargetRotation,
            smoothProgress
        );

        if (progress < 1f)
            return;

        transform.position = traversalTargetPosition;
        transform.rotation = traversalTargetRotation;

        traversalPhase = TraversalPhase.Hang;

        if (playerLook != null)
        {
            playerLook.EnterHangLook();
            playerLook.enabled = true;
        }
    }

    private void UpdateHang()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            ReleaseHang();
            return;
        }

        UpdateShimmy();

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (
            !currentClimbZone.TryGetNearestPullUpPoint(
                transform.position,
                out PullUpPoint pullUpPoint
            )
        )
        {
            return;
        }

        currentPullUpPoint = pullUpPoint;

        if (playerLook != null)
            playerLook.enabled = false;

        StartTraversalPhase(
            TraversalPhase.MoveToPullUp,
            currentPullUpPoint.HangPoint.position
        );
    }

    private void UpdateShimmy()
    {
        float input = 0f;

        if (Keyboard.current.aKey.isPressed)
            input -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input += 1f;

        if (Mathf.Abs(input) <= 0.001f)
            return;

        Vector3 start = currentClimbZone.ShimmyStart.position;
        Vector3 end = currentClimbZone.ShimmyEnd.position;

        Vector3 segment = end - start;
        float segmentLength = segment.magnitude;

        if (segmentLength <= 0.001f)
            return;

        Vector3 segmentDirection = segment / segmentLength;

        Vector3 approachDirection =
            currentClimbZone.ApproachDirection;

        approachDirection.y = 0f;

        if (approachDirection.sqrMagnitude <= 0.001f)
            return;

        approachDirection.Normalize();

        Vector3 ledgeRight = Vector3.Cross(
            Vector3.up,
            approachDirection
        ).normalized;

        float segmentRightSign = Mathf.Sign(
            Vector3.Dot(segmentDirection, ledgeRight)
        );

        if (Mathf.Abs(segmentRightSign) <= 0.001f)
            return;

        float currentDistance = Vector3.Dot(
            transform.position - start,
            segmentDirection
        );

        currentDistance +=
            input *
            shimmySpeed *
            segmentRightSign *
            Time.deltaTime;

        currentDistance = Mathf.Clamp(
            currentDistance,
            0f,
            segmentLength
        );

        transform.position =
            start +
            segmentDirection * currentDistance;
    }

    private void UpdateMoveToPullUp()
    {
        traversalTime += Time.deltaTime;

        float progress = GetProgress(
            traversalTime,
            moveToPullUpDuration
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        transform.position = Vector3.Lerp(
            traversalStartPosition,
            traversalTargetPosition,
            smoothProgress
        );

        if (progress < 1f)
            return;

        transform.position = traversalTargetPosition;

        StartTraversalPhase(
            TraversalPhase.PullUp,
            currentPullUpPoint.StandPoint.position
        );
    }

    private void UpdatePullUp()
    {
        traversalTime += Time.deltaTime;

        float progress = GetProgress(
            traversalTime,
            pullUpDuration
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        transform.position = Vector3.Lerp(
            traversalStartPosition,
            traversalTargetPosition,
            smoothProgress
        );

        if (progress < 1f)
            return;

        FinishTraversal();
    }

    private void StartTraversalPhase(
        TraversalPhase phase,
        Vector3 targetPosition
    )
    {
        traversalPhase = phase;
        traversalTime = 0f;

        traversalStartPosition = transform.position;
        traversalTargetPosition = targetPosition;
    }

    private float GetProgress(
        float elapsedTime,
        float duration
    )
    {
        if (duration <= 0f)
            return 1f;

        return Mathf.Clamp01(elapsedTime / duration);
    }

    private void ReleaseHang()
    {
        traversalPhase = TraversalPhase.None;
        isTraversing = false;

        currentPullUpPoint = null;

        playerMovement.ResetTraversalVelocity();

        characterController.enabled = true;
        playerMovement.enabled = true;

        if (playerCameraMotion != null)
            playerCameraMotion.SetSuppressed(false);

        if (playerLook != null)
        {
            playerLook.ExitHangLookPreservingView();
            playerLook.enabled = true;
        }
    }

    private void FinishTraversal()
    {
        Vector3 standPosition =
            currentPullUpPoint.StandPoint.position;

        transform.position = standPosition;

        traversalPhase = TraversalPhase.None;
        isTraversing = false;

        currentPullUpPoint = null;
        currentClimbZone = null;

        playerMovement.ResetTraversalVelocity();

        characterController.enabled = true;
        playerMovement.enabled = true;

        if (playerCameraMotion != null)
            playerCameraMotion.SetSuppressed(false);

        if (playerLook != null)
        {
            playerLook.ExitHangLookPreservingView();
            playerLook.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ClimbZone climbZone = other.GetComponent<ClimbZone>();

        if (climbZone == null)
            return;

        currentClimbZone = climbZone;

        if (
            !playerMovement.IsGrounded &&
            !isTraversing &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed &&
            IsMovingTowardClimbZone(climbZone)
        )
        {
            TryStartClimb();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ClimbZone climbZone = other.GetComponent<ClimbZone>();

        if (
            climbZone == currentClimbZone &&
            !isTraversing
        )
        {
            currentClimbZone = null;
        }
    }
}