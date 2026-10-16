using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class TraversalMotor : MonoBehaviour
{
    [SerializeField] private TraversalProfile traversalProfile;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private TraversalDetector traversalDetector;
    [SerializeField] private TraversalResolver traversalResolver;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;

    private CharacterController characterController;
    private Coroutine traversalRoutine;
    private bool HasEnhancedMobility => memberAssembly != null && enhancedMobilityCapability != null && memberAssembly.HasCapability(enhancedMobilityCapability);

    public bool IsTraversing => traversalRoutine != null;

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
        Debug.Log($"Traversal: {traversalType}, height: {candidate.Height:F2}");

        if (traversalType == TraversalType.Vault)
            StartVault(candidate, traversalResolver.LandingPoint);
    }

    private void StartVault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        traversalRoutine = StartCoroutine(Vault(candidate, landingPoint));
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