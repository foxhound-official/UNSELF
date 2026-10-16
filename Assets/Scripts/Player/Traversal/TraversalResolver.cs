using UnityEngine;

public class TraversalResolver : MonoBehaviour
{
    [Header("Vault")]
    [SerializeField] private float maximumVaultHeight = 1.2f;
    [SerializeField] private float maximumVaultDepth = 1f;
    [SerializeField] private float vaultDepthStep = 0.1f;

    [Header("Mantle")]
    [SerializeField] private float maximumMantleHeight = 2.2f;

    [Header("Landing")]
    [SerializeField] private float landingSearchHeight = 1.5f;
    [SerializeField] private float landingSearchDistance = 0.5f;
    [SerializeField] private float landingClearanceHeight = 1.8f;
    [SerializeField] private float landingClearanceRadius = 0.3f;
    [SerializeField] private float landingEdgeClearance = 0.1f;

    [Header("Collision")]
    [SerializeField] private LayerMask traversalMask = Physics.DefaultRaycastLayers;

    private CharacterController characterController;

    public TraversalType CurrentType { get; private set; }
    public Vector3 LandingPoint { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public TraversalType Resolve(TraversalCandidate candidate)
    {
        CurrentType = TraversalType.None;
        LandingPoint = Vector3.zero;

        if (candidate.Height <= maximumVaultHeight && TryResolveVault(candidate, out Vector3 vaultLandingPoint))
        {
            CurrentType = TraversalType.Vault;
            LandingPoint = vaultLandingPoint;
            return CurrentType;
        }

        if (candidate.Height <= maximumMantleHeight && HasMantleClearance(candidate))
        {
            CurrentType = TraversalType.Mantle;
            LandingPoint = candidate.TopPoint;
            return CurrentType;
        }

        return CurrentType;
    }
    private bool TryResolveVault(TraversalCandidate candidate, out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;

        Vector3 forward = -candidate.ObstacleNormal;
        Vector3 lastObstaclePoint = candidate.TopPoint;

        for (float depth = vaultDepthStep; depth <= maximumVaultDepth; depth += vaultDepthStep)
        {
            Vector3 samplePosition = candidate.TopPoint + forward * depth;
            Vector3 rayOrigin = samplePosition + Vector3.up * landingSearchHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, landingSearchHeight * 2f, traversalMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == candidate.Collider)
                {
                    lastObstaclePoint = hit.point;
                    continue;
                }

                float landingOffset = characterController.radius + characterController.skinWidth + landingEdgeClearance;
                Vector3 landingSearchOrigin = samplePosition + forward * landingOffset + Vector3.up * landingSearchHeight;

                if (Physics.Raycast(landingSearchOrigin, Vector3.down, out RaycastHit landingHit, landingSearchHeight * 2f, traversalMask, QueryTriggerInteraction.Ignore) &&
                    TryValidateLanding(landingHit.point, out landingPoint))
                    return true;
            }
            else
            {
                Vector3 searchOrigin = lastObstaclePoint + forward * landingSearchDistance + Vector3.up * landingSearchHeight;

                if (Physics.Raycast(searchOrigin, Vector3.down, out RaycastHit landingHit, landingSearchHeight * 2f, traversalMask, QueryTriggerInteraction.Ignore))
                {
                    bool validLanding = TryValidateLanding(landingHit.point, out landingPoint);

                    Debug.Log($"Vault fallback: hit {landingHit.collider.name}, valid = {validLanding}");

                    if (validLanding)
                        return true;
                }
                else
                {
                    Debug.Log("Vault fallback: no ground hit");
                }
            }
        }

        Debug.Log("Vault resolution failed");
        return false;
    }

    private bool HasMantleClearance(TraversalCandidate candidate)
    {
        float radius = landingClearanceRadius;
        float height = Mathf.Max(landingClearanceHeight, radius * 2f);

        Vector3 bottom = candidate.TopPoint + Vector3.up * radius;
        Vector3 top = candidate.TopPoint + Vector3.up * (height - radius);

        return !Physics.CheckCapsule(bottom, top, radius, traversalMask, QueryTriggerInteraction.Ignore);
    }

    private bool TryValidateLanding(Vector3 point, out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;

        float radius = landingClearanceRadius;
        float height = Mathf.Max(landingClearanceHeight, radius * 2f);
        float clearanceOffset = characterController.skinWidth;

        Vector3 bottom = point + Vector3.up * (radius + clearanceOffset);
        Vector3 top = point + Vector3.up * (height - radius + clearanceOffset);

        if (Physics.CheckCapsule(bottom, top, radius, traversalMask, QueryTriggerInteraction.Ignore))
            return false;

        landingPoint = point;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (CurrentType == TraversalType.None)
            return;

        Gizmos.DrawWireSphere(LandingPoint, landingClearanceRadius);
    }
}