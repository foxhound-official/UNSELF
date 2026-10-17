using KinematicCharacterController;
using UnityEngine;

public class TraversalResolver : MonoBehaviour
{
    private const float CollisionClearance = 0.01f;

    [Header("Approach")]
    [SerializeField] private float maximumApproachAngle = 60f;

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
    [SerializeField] private float maximumLandingSurfaceAngle = 45f;

    [Header("Collision")]
    [SerializeField] private LayerMask traversalMask = Physics.DefaultRaycastLayers;

    private KinematicCharacterMotor motor;

    public TraversalType CurrentType { get; private set; }
    public Vector3 LandingPoint { get; private set; }

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    public TraversalType Resolve(
        TraversalCandidate candidate)
    {
        CurrentType = TraversalType.None;
        LandingPoint = Vector3.zero;

        if (!IsValidApproach(candidate))
            return CurrentType;

        if (candidate.Height <= maximumVaultHeight &&
            TryResolveVault(
                candidate,
                out Vector3 vaultLandingPoint))
        {
            CurrentType = TraversalType.Vault;
            LandingPoint = vaultLandingPoint;

            return CurrentType;
        }

        if (candidate.Height <= maximumMantleHeight &&
            TryResolveMantle(
                candidate,
                out Vector3 mantleLandingPoint))
        {
            CurrentType = TraversalType.Mantle;
            LandingPoint = mantleLandingPoint;

            return CurrentType;
        }

        return CurrentType;
    }

    private bool IsValidApproach(
        TraversalCandidate candidate)
    {
        Vector3 playerForward =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up).normalized;

        Vector3 traversalDirection =
            Vector3.ProjectOnPlane(
                -candidate.ObstacleNormal,
                Vector3.up).normalized;

        if (traversalDirection.sqrMagnitude <= 0.0001f)
            return false;

        return Vector3.Angle(
                   playerForward,
                   traversalDirection) <=
               maximumApproachAngle;
    }

    private bool TryResolveVault(
        TraversalCandidate candidate,
        out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;

        Vector3 forward =
            -candidate.ObstacleNormal;

        Vector3 lastObstaclePoint =
            candidate.TopPoint;

        for (float depth = vaultDepthStep;
             depth <= maximumVaultDepth;
             depth += vaultDepthStep)
        {
            Vector3 samplePosition =
                candidate.TopPoint +
                forward * depth;

            Vector3 rayOrigin =
                samplePosition +
                Vector3.up *
                landingSearchHeight;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    landingSearchHeight * 2f,
                    traversalMask,
                    QueryTriggerInteraction.Ignore))
            {
                if (hit.collider ==
                    candidate.Collider)
                {
                    lastObstaclePoint =
                        hit.point;

                    continue;
                }

                float landingOffset =
                    motor.Capsule.radius +
                    CollisionClearance +
                    landingEdgeClearance;

                Vector3 landingSearchOrigin =
                    samplePosition +
                    forward * landingOffset +
                    Vector3.up *
                    landingSearchHeight;

                if (Physics.Raycast(
                        landingSearchOrigin,
                        Vector3.down,
                        out RaycastHit landingHit,
                        landingSearchHeight * 2f,
                        traversalMask,
                        QueryTriggerInteraction.Ignore) &&
                    TryValidateLanding(
                        landingHit,
                        out landingPoint))
                {
                    return true;
                }
            }
            else
            {
                Vector3 searchOrigin =
                    lastObstaclePoint +
                    forward *
                    landingSearchDistance +
                    Vector3.up *
                    landingSearchHeight;

                if (Physics.Raycast(
                        searchOrigin,
                        Vector3.down,
                        out RaycastHit landingHit,
                        landingSearchHeight * 2f,
                        traversalMask,
                        QueryTriggerInteraction.Ignore))
                {
                    if (TryValidateLanding(
                            landingHit,
                            out landingPoint))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryResolveMantle(
        TraversalCandidate candidate,
        out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;

        Vector3 forward =
            -candidate.ObstacleNormal;

        float landingOffset =
            motor.Capsule.radius +
            CollisionClearance +
            landingEdgeClearance;

        Vector3 searchOrigin =
            candidate.TopPoint +
            forward * landingOffset +
            Vector3.up *
            landingSearchHeight;

        if (!Physics.Raycast(
                searchOrigin,
                Vector3.down,
                out RaycastHit hit,
                landingSearchHeight * 2f,
                traversalMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return TryValidateLanding(
            hit,
            out landingPoint);
    }

    private bool TryValidateLanding(
        RaycastHit hit,
        out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;

        float surfaceAngle =
            Vector3.Angle(
                hit.normal,
                Vector3.up);

        if (surfaceAngle >
            maximumLandingSurfaceAngle)
        {
            return false;
        }

        float radius =
            landingClearanceRadius;

        float height =
            Mathf.Max(
                landingClearanceHeight,
                radius * 2f);

        Vector3 bottom =
            hit.point +
            Vector3.up *
            (radius + CollisionClearance);

        Vector3 top =
            hit.point +
            Vector3.up *
            (height -
             radius +
             CollisionClearance);

        if (Physics.CheckCapsule(
                bottom,
                top,
                radius,
                traversalMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        landingPoint = hit.point;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (CurrentType ==
            TraversalType.None)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            LandingPoint,
            landingClearanceRadius);
    }
}