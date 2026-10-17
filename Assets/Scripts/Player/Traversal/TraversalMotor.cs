using System.Collections;
using System;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class TraversalMotor : MonoBehaviour
{
    [SerializeField] private TraversalProfile traversalProfile;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private TraversalDetector traversalDetector;
    [SerializeField] private TraversalResolver traversalResolver;
    [SerializeField] private TraversalPathValidator pathValidator;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;

    private CharacterController characterController;
    private Coroutine traversalRoutine;
    private bool HasEnhancedMobility => memberAssembly != null && enhancedMobilityCapability != null && memberAssembly.HasCapability(enhancedMobilityCapability);

    public bool IsTraversing => traversalRoutine != null;
    public event Action<TraversalType> TraversalStarted;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (IsTraversing || !inputReader.JumpPressed)
            return;

        if (!playerMovement.IsGrounded || playerMovement.IsCrouching || playerMovement.IsSliding)
            return;

        if (!traversalDetector.TryDetect(out TraversalCandidate candidate))
            return;

        TraversalType traversalType = traversalResolver.Resolve(candidate);

        if (traversalType == TraversalType.Vault)
            StartVault(candidate, traversalResolver.LandingPoint);
        else if (traversalType == TraversalType.Mantle)
            StartMantle(candidate, traversalResolver.LandingPoint);
    }

    private void StartMantle(TraversalCandidate candidate, Vector3 landingPoint)
    {
        Vector3 startPoint = transform.position;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        float wallOffset = characterController.radius + traversalProfile.MantleWallClearance;

        Vector3 wallPoint = candidate.ObstaclePoint + candidate.ObstacleNormal * wallOffset;
        wallPoint.y = startPoint.y;

        Vector3 pullPoint = wallPoint + traversalDirection * traversalProfile.MantlePullForward;
        pullPoint.y = candidate.TopPoint.y - characterController.radius;

        Vector3 crestPoint = candidate.TopPoint + traversalDirection * traversalProfile.MantleCrestForward;
        crestPoint.y = candidate.TopPoint.y + traversalProfile.MantleTopClearance;

        Vector3 settlePoint = landingPoint + Vector3.up * traversalProfile.MantleSettleHeight;
        Vector3 finalLandingPoint = landingPoint + Vector3.up * characterController.skinWidth;

        if (!pathValidator.IsSegmentClear(startPoint, wallPoint, candidate.Collider))
            return;

        if (!pathValidator.IsSegmentClear(wallPoint, pullPoint, candidate.Collider))
            return;

        if (!pathValidator.IsQuadraticBezierClear(pullPoint, crestPoint, settlePoint, candidate.Collider))
            return;

        if (!pathValidator.IsSegmentClear(settlePoint, finalLandingPoint, candidate.Collider))
            return;

        TraversalStarted?.Invoke(TraversalType.Mantle);
        traversalRoutine = StartCoroutine(Mantle(candidate, landingPoint));
    }

    private void StartVault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        Vector3 startPoint = transform.position;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        Vector3 entryPoint = candidate.ObstaclePoint + candidate.ObstacleNormal * (characterController.radius + traversalProfile.VaultEntryClearance);
        entryPoint.y = startPoint.y + traversalProfile.VaultEntryRise;

        Vector3 clearancePoint = candidate.TopPoint + traversalDirection * traversalProfile.VaultForwardClearance;
        clearancePoint.y = candidate.TopPoint.y + traversalProfile.VaultTopClearance;

        if (!pathValidator.IsSegmentClear(startPoint, entryPoint, candidate.Collider))
            return;

        if (!pathValidator.IsSegmentClear(entryPoint, clearancePoint, candidate.Collider))
            return;

        if (!pathValidator.IsSegmentClear(clearancePoint, landingPoint, candidate.Collider))
            return;

        TraversalStarted?.Invoke(TraversalType.Vault);
        traversalRoutine = StartCoroutine(Vault(candidate, landingPoint));
    }

    private IEnumerator Mantle(TraversalCandidate candidate, Vector3 landingPoint)
    {
        float entrySpeed = playerMovement.HorizontalSpeed;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        playerMovement.SetMovementMode(PlayerMovementMode.Traversal);
        characterController.detectCollisions = false;

        try
        {
            Vector3 startPoint = transform.position;

            float wallOffset = characterController.radius + traversalProfile.MantleWallClearance;

            Vector3 wallPoint = candidate.ObstaclePoint + candidate.ObstacleNormal * wallOffset;
            wallPoint.y = startPoint.y;

            Vector3 pullPoint = wallPoint + traversalDirection * traversalProfile.MantlePullForward;
            pullPoint.y = candidate.TopPoint.y - characterController.radius;

            Vector3 crestPoint = candidate.TopPoint + traversalDirection * traversalProfile.MantleCrestForward;
            crestPoint.y = candidate.TopPoint.y + traversalProfile.MantleTopClearance;

            Vector3 settlePoint = landingPoint + Vector3.up * traversalProfile.MantleSettleHeight;

            float approachDistance = Vector3.Distance(startPoint, wallPoint);
            float pullDistance = Vector3.Distance(wallPoint, pullPoint);
            float crestDistance = Vector3.Distance(pullPoint, crestPoint);
            float settleDistance = Vector3.Distance(crestPoint, settlePoint);
            float landingDistance = Vector3.Distance(settlePoint, landingPoint);

            float totalDistance = approachDistance + pullDistance + crestDistance + settleDistance + landingDistance;

            float traversalSpeed = Mathf.Max(entrySpeed, traversalProfile.MantleMinimumTraversalSpeed);
            float duration = Mathf.Clamp(totalDistance / traversalSpeed, traversalProfile.MantleMinimumDuration, traversalProfile.MantleMaximumDuration);

            float approachEnd = approachDistance / totalDistance;
            float pullEnd = (approachDistance + pullDistance) / totalDistance;
            float settleEnd = (approachDistance + pullDistance + crestDistance + settleDistance) / totalDistance;

            float elapsed = 0f;
            Vector3 finalLandingPoint = landingPoint + Vector3.up * characterController.skinWidth;

            while (true)
            {
                elapsed += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float progress = traversalProfile.MantleProgress.Evaluate(normalizedTime);

                Vector3 targetPosition;

                if (progress < approachEnd)
                {
                    float segmentProgress = progress / approachEnd;
                    targetPosition = Vector3.Lerp(startPoint, wallPoint, segmentProgress);
                }
                else if (progress < pullEnd)
                {
                    float segmentProgress = (progress - approachEnd) / (pullEnd - approachEnd);
                    targetPosition = Vector3.Lerp(wallPoint, pullPoint, segmentProgress);
                }
                else if (progress < settleEnd)
                {
                    float segmentProgress = (progress - pullEnd) / (settleEnd - pullEnd);
                    targetPosition = EvaluateQuadraticBezier(pullPoint, crestPoint, settlePoint, segmentProgress);
                }
                else
                {
                    float segmentProgress = (progress - settleEnd) / (1f - settleEnd);
                    targetPosition = Vector3.Lerp(settlePoint, finalLandingPoint, segmentProgress);
                }

                transform.position = targetPosition;

                if (normalizedTime >= 1f)
                    break;

                yield return null;
            }
        }
        finally
        {
            characterController.detectCollisions = true;

            Vector3 exitVelocity = traversalDirection * entrySpeed;
            playerMovement.CompleteTraversal(exitVelocity);

            traversalRoutine = null;
        }
    }

    private Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float progress)
    {
        float inverseProgress = 1f - progress;
        return inverseProgress * inverseProgress * start + 2f * inverseProgress * progress * control + progress * progress * end;
    }

    private IEnumerator Vault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        float entrySpeed = playerMovement.HorizontalSpeed;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        playerMovement.SetMovementMode(PlayerMovementMode.Traversal);
        characterController.detectCollisions = false;

        try
        {
            Vector3 startPoint = transform.position;

            Vector3 entryPoint = candidate.ObstaclePoint + candidate.ObstacleNormal * (characterController.radius + traversalProfile.VaultEntryClearance);
            entryPoint.y = startPoint.y + traversalProfile.VaultEntryRise;

            Vector3 clearancePoint = candidate.TopPoint + traversalDirection * traversalProfile.VaultForwardClearance;
            clearancePoint.y = candidate.TopPoint.y + traversalProfile.VaultTopClearance;

            float entryDistance = Vector3.Distance(startPoint, entryPoint);
            float clearanceDistance = Vector3.Distance(entryPoint, clearancePoint);
            float landingDistance = Vector3.Distance(clearancePoint, landingPoint);
            float totalDistance = entryDistance + clearanceDistance + landingDistance;

            float traversalSpeed = Mathf.Max(entrySpeed, traversalProfile.VaultMinimumTraversalSpeed);
            float duration = Mathf.Clamp(totalDistance / traversalSpeed, traversalProfile.VaultMinimumDuration, traversalProfile.VaultMaximumDuration);

            float entryEnd = entryDistance / totalDistance;
            float clearanceEnd = (entryDistance + clearanceDistance) / totalDistance;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float progress = traversalProfile.VaultProgress.Evaluate(normalizedTime);

                Vector3 targetPosition;

                if (progress < entryEnd)
                {
                    float segmentProgress = progress / entryEnd;
                    targetPosition = Vector3.Lerp(startPoint, entryPoint, segmentProgress);
                }
                else if (progress < clearanceEnd)
                {
                    float segmentProgress = (progress - entryEnd) / (clearanceEnd - entryEnd);
                    targetPosition = Vector3.Lerp(entryPoint, clearancePoint, segmentProgress);
                }
                else
                {
                    float segmentProgress = (progress - clearanceEnd) / (1f - clearanceEnd);
                    targetPosition = Vector3.Lerp(clearancePoint, landingPoint, segmentProgress);
                }

                transform.position = targetPosition;

                yield return null;
            }

            transform.position = landingPoint;
        }
        finally
        {
            characterController.detectCollisions = true;

            float exitSpeedBoost = HasEnhancedMobility ? traversalProfile.EnhancedVaultExitSpeedBoost : traversalProfile.VaultExitSpeedBoost;
            float exitSpeed = entrySpeed + exitSpeedBoost;

            Vector3 exitVelocity = traversalDirection * exitSpeed;
            playerMovement.CompleteTraversal(exitVelocity);

            traversalRoutine = null;
        }
    }
}