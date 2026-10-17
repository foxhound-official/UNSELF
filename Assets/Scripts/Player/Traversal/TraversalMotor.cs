using System;
using System.Collections;
using KinematicCharacterController;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class TraversalMotor : MonoBehaviour
{
    private const float LandingSurfaceOffset = 0.02f;

    [SerializeField] private TraversalProfile traversalProfile;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TraversalDetector traversalDetector;
    [SerializeField] private TraversalResolver traversalResolver;
    [SerializeField] private TraversalPathValidator pathValidator;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;

    private KinematicCharacterMotor motor;
    private Coroutine traversalRoutine;

    private bool HasEnhancedMobility =>
        memberAssembly != null &&
        enhancedMobilityCapability != null &&
        memberAssembly.HasCapability(enhancedMobilityCapability);

    public bool IsTraversing => traversalRoutine != null;

    public event Action<TraversalType> TraversalStarted;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    public bool TryStartTraversal()
    {
        if (IsTraversing)
            return false;

        if (motor == null || playerMovement == null || traversalDetector == null ||
            traversalResolver == null || pathValidator == null)
            return false;

        if (playerMovement.MovementMode != PlayerMovementMode.Locomotion)
            return false;

        if (!playerMovement.IsGrounded || playerMovement.IsCrouching || playerMovement.IsSliding)
            return false;

        if (!traversalDetector.TryDetect(out TraversalCandidate candidate))
            return false;

        TraversalType traversalType = traversalResolver.Resolve(candidate);

        if (traversalType == TraversalType.Vault)
            return TryStartVault(candidate, traversalResolver.LandingPoint);

        if (traversalType == TraversalType.Mantle)
            return TryStartMantle(candidate, traversalResolver.LandingPoint);

        return false;
    }

    private bool TryStartMantle(TraversalCandidate candidate, Vector3 landingPoint)
    {
        Vector3 startPoint = motor.TransientPosition;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        float wallOffset = motor.Capsule.radius + traversalProfile.MantleWallClearance;

        Vector3 wallPoint = candidate.ObstaclePoint + candidate.ObstacleNormal * wallOffset;
        wallPoint.y = startPoint.y;

        Vector3 pullPoint = wallPoint + traversalDirection * traversalProfile.MantlePullForward;
        pullPoint.y = candidate.TopPoint.y - motor.Capsule.radius;

        Vector3 crestPoint = candidate.TopPoint + traversalDirection * traversalProfile.MantleCrestForward;
        crestPoint.y = candidate.TopPoint.y + traversalProfile.MantleTopClearance;

        Vector3 settlePoint = landingPoint + Vector3.up * traversalProfile.MantleSettleHeight;
        Vector3 finalLandingPoint = landingPoint + Vector3.up * LandingSurfaceOffset;

        if (!pathValidator.IsSegmentClear(startPoint, wallPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(wallPoint, pullPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsQuadraticBezierClear(pullPoint, crestPoint, settlePoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(settlePoint, finalLandingPoint, candidate.Collider))
            return false;

        TraversalStarted?.Invoke(TraversalType.Mantle);
        traversalRoutine = StartCoroutine(Mantle(candidate, landingPoint));

        return true;
    }

    private bool TryStartVault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        Vector3 startPoint = motor.TransientPosition;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        Vector3 entryPoint = candidate.ObstaclePoint +
                             candidate.ObstacleNormal *
                             (motor.Capsule.radius + traversalProfile.VaultEntryClearance);

        entryPoint.y = startPoint.y + traversalProfile.VaultEntryRise;

        Vector3 clearancePoint = candidate.TopPoint +
                                 traversalDirection *
                                 traversalProfile.VaultForwardClearance;

        clearancePoint.y = candidate.TopPoint.y + traversalProfile.VaultTopClearance;

        Vector3 finalLandingPoint = landingPoint + Vector3.up * LandingSurfaceOffset;

        if (!pathValidator.IsSegmentClear(startPoint, entryPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(entryPoint, clearancePoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(clearancePoint, finalLandingPoint, candidate.Collider))
            return false;

        TraversalStarted?.Invoke(TraversalType.Vault);
        traversalRoutine = StartCoroutine(Vault(candidate, landingPoint));

        return true;
    }

    private IEnumerator Mantle(TraversalCandidate candidate, Vector3 landingPoint)
    {
        float entrySpeed = playerMovement.HorizontalSpeed;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        playerMovement.SetMovementMode(PlayerMovementMode.Traversal);
        BeginAuthoredTraversal();

        try
        {
            Vector3 startPoint = motor.TransientPosition;

            float wallOffset = motor.Capsule.radius + traversalProfile.MantleWallClearance;

            Vector3 wallPoint = candidate.ObstaclePoint + candidate.ObstacleNormal * wallOffset;
            wallPoint.y = startPoint.y;

            Vector3 pullPoint = wallPoint + traversalDirection * traversalProfile.MantlePullForward;
            pullPoint.y = candidate.TopPoint.y - motor.Capsule.radius;

            Vector3 crestPoint = candidate.TopPoint + traversalDirection * traversalProfile.MantleCrestForward;
            crestPoint.y = candidate.TopPoint.y + traversalProfile.MantleTopClearance;

            Vector3 settlePoint = landingPoint + Vector3.up * traversalProfile.MantleSettleHeight;
            Vector3 finalLandingPoint = landingPoint + Vector3.up * LandingSurfaceOffset;

            float approachDistance = Vector3.Distance(startPoint, wallPoint);
            float pullDistance = Vector3.Distance(wallPoint, pullPoint);
            float crestDistance = Vector3.Distance(pullPoint, crestPoint);
            float settleDistance = Vector3.Distance(crestPoint, settlePoint);
            float landingDistance = Vector3.Distance(settlePoint, finalLandingPoint);

            float totalDistance =
                approachDistance + pullDistance + crestDistance + settleDistance + landingDistance;

            float traversalSpeed = Mathf.Max(entrySpeed, traversalProfile.MantleMinimumTraversalSpeed);
            float duration = Mathf.Clamp(
                totalDistance / traversalSpeed,
                traversalProfile.MantleMinimumDuration,
                traversalProfile.MantleMaximumDuration
            );

            float approachEnd = approachDistance / totalDistance;
            float pullEnd = (approachDistance + pullDistance) / totalDistance;
            float settleEnd =
                (approachDistance + pullDistance + crestDistance + settleDistance) / totalDistance;

            float elapsed = 0f;

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
                    targetPosition = EvaluateQuadraticBezier(
                        pullPoint,
                        crestPoint,
                        settlePoint,
                        segmentProgress
                    );
                }
                else
                {
                    float segmentProgress = (progress - settleEnd) / (1f - settleEnd);
                    targetPosition = Vector3.Lerp(settlePoint, finalLandingPoint, segmentProgress);
                }

                motor.SetPosition(targetPosition);

                if (normalizedTime >= 1f)
                    break;

                yield return null;
            }

            motor.SetPosition(finalLandingPoint);
        }
        finally
        {
            EndAuthoredTraversal();

            Vector3 exitVelocity = traversalDirection * entrySpeed;
            playerMovement.CompleteTraversal(exitVelocity);

            traversalRoutine = null;
        }
    }

    private IEnumerator Vault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        float entrySpeed = playerMovement.HorizontalSpeed;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        playerMovement.SetMovementMode(PlayerMovementMode.Traversal);
        BeginAuthoredTraversal();

        try
        {
            Vector3 startPoint = motor.TransientPosition;

            Vector3 entryPoint = candidate.ObstaclePoint +
                                 candidate.ObstacleNormal *
                                 (motor.Capsule.radius + traversalProfile.VaultEntryClearance);

            entryPoint.y = startPoint.y + traversalProfile.VaultEntryRise;

            Vector3 clearancePoint = candidate.TopPoint +
                                     traversalDirection *
                                     traversalProfile.VaultForwardClearance;

            clearancePoint.y = candidate.TopPoint.y + traversalProfile.VaultTopClearance;

            Vector3 finalLandingPoint = landingPoint + Vector3.up * LandingSurfaceOffset;

            float entryDistance = Vector3.Distance(startPoint, entryPoint);
            float clearanceDistance = Vector3.Distance(entryPoint, clearancePoint);
            float landingDistance = Vector3.Distance(clearancePoint, finalLandingPoint);
            float totalDistance = entryDistance + clearanceDistance + landingDistance;

            float traversalSpeed = Mathf.Max(entrySpeed, traversalProfile.VaultMinimumTraversalSpeed);
            float duration = Mathf.Clamp(
                totalDistance / traversalSpeed,
                traversalProfile.VaultMinimumDuration,
                traversalProfile.VaultMaximumDuration
            );

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
                    targetPosition = Vector3.Lerp(clearancePoint, finalLandingPoint, segmentProgress);
                }

                motor.SetPosition(targetPosition);

                yield return null;
            }

            motor.SetPosition(finalLandingPoint);
        }
        finally
        {
            EndAuthoredTraversal();

            float exitSpeedBoost = HasEnhancedMobility
                ? traversalProfile.EnhancedVaultExitSpeedBoost
                : traversalProfile.VaultExitSpeedBoost;

            float exitSpeed = entrySpeed + exitSpeedBoost;
            Vector3 exitVelocity = traversalDirection * exitSpeed;

            playerMovement.CompleteTraversal(exitVelocity);

            traversalRoutine = null;
        }
    }

    private void BeginAuthoredTraversal()
    {
        // The path has already been validated, so KCC collision solving must not fight the authored motion.
        motor.BaseVelocity = Vector3.zero;
        motor.SetMovementCollisionsSolvingActivation(false);
        motor.SetGroundSolvingActivation(false);
    }

    private void EndAuthoredTraversal()
    {
        motor.SetMovementCollisionsSolvingActivation(true);
        motor.SetGroundSolvingActivation(true);
    }

    private Vector3 EvaluateQuadraticBezier(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float progress)
    {
        float inverseProgress = 1f - progress;

        return inverseProgress * inverseProgress * start +
               2f * inverseProgress * progress * control +
               progress * progress * end;
    }
}