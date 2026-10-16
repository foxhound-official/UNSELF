using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTraversal : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private MemberAssembly memberAssembly;

    [Header("Climb")]
    [SerializeField] private float moveToHangDuration = 0.3f;
    [SerializeField] private float pullUpDuration = 0.8f;

    private CharacterController characterController;
    private ClimbZone currentClimbZone;

    private bool isTraversing;
    private float traversalTime;

    private Vector3 traversalStartPosition;
    private Vector3 traversalTargetPosition;

    private TraversalPhase traversalPhase;

    private enum TraversalPhase
    {
        None,
        MoveToHang,
        PullUp
    }

    public bool IsTraversing => isTraversing;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        Debug.Log(
            $"PLAYER TRAVERSAL READY | " +
            $"Object: {gameObject.name} | " +
            $"Path: {GetHierarchyPath(transform)} | " +
            $"CharacterController: {characterController != null} | " +
            $"Movement: {playerMovement != null} | " +
            $"Look: {playerLook != null} | " +
            $"Assembly: {memberAssembly != null}"
        );
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

        Debug.Log(
            $"CLIMB START | Player: {transform.position} | " +
            $"Hang: {currentClimbZone.HangPoint.position} | " +
            $"Stand: {currentClimbZone.StandPoint.position}"
        );

        isTraversing = true;

        playerMovement.enabled = false;

        if (playerLook != null)
            playerLook.enabled = false;

        // Traversal follows authored points directly, so normal
        // CharacterController collision resolution is suspended temporarily.
        characterController.enabled = false;

        StartTraversalPhase(
            TraversalPhase.MoveToHang,
            currentClimbZone.HangPoint.position
        );
    }

    private bool CanUseClimbZone(ClimbZone climbZone)
    {
        if (climbZone == null)
            return false;

        if (climbZone.HangPoint == null || climbZone.StandPoint == null)
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
        Vector3 toZone = climbZone.transform.position - transform.position;
        toZone.y = 0f;

        if (toZone.sqrMagnitude <= 0.001f)
            return true;

        Vector3 approachDirection = climbZone.ApproachDirection;
        approachDirection.y = 0f;

        if (approachDirection.sqrMagnitude <= 0.001f)
            return true;

        toZone.Normalize();
        approachDirection.Normalize();

        float approachDot = Vector3.Dot(toZone, approachDirection);

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

    private void UpdateTraversal()
    {
        traversalTime += Time.deltaTime;

        float duration = traversalPhase == TraversalPhase.MoveToHang
            ? moveToHangDuration
            : pullUpDuration;

        float progress = duration > 0f
            ? Mathf.Clamp01(traversalTime / duration)
            : 1f;

        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

        transform.position = Vector3.Lerp(
            traversalStartPosition,
            traversalTargetPosition,
            smoothProgress
        );

        if (progress < 1f)
            return;

        CompleteTraversalPhase();
    }

    private void CompleteTraversalPhase()
    {
        if (traversalPhase == TraversalPhase.MoveToHang)
        {
            StartTraversalPhase(
                TraversalPhase.PullUp,
                currentClimbZone.StandPoint.position
            );

            return;
        }

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

    private void FinishTraversal()
    {
        Vector3 standPosition = currentClimbZone.StandPoint.position;

        transform.position = standPosition;

        traversalPhase = TraversalPhase.None;
        isTraversing = false;

        // The traversal zone may have been exited while CharacterController
        // was disabled, so clear the cached zone explicitly.
        currentClimbZone = null;

        characterController.enabled = true;
        playerMovement.enabled = true;

        if (playerLook != null)
            playerLook.enabled = true;
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

        if (climbZone == currentClimbZone && !isTraversing)
            currentClimbZone = null;
    }

    private static string GetHierarchyPath(Transform current)
    {
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.name}/{path}";
        }

        return path;
    }
}