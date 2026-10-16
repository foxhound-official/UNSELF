using UnityEngine;

public class TraversalDetector : MonoBehaviour
{
    private const int HitCapacity = 8;

    [Header("Obstacle Detection")]
    [SerializeField] private float obstacleProbeHeight = 1f;
    [SerializeField] private float obstacleProbeRadius = 0.15f;
    [SerializeField] private float obstacleProbeDistance = 1f;

    [Header("Top Detection")]
    [SerializeField] private float maximumTraversalHeight = 2.2f;
    [SerializeField] private float minimumTraversalHeight = 0.25f;
    [SerializeField] private float topSearchStartDepth = 0.03f;
    [SerializeField] private float topSearchMaximumDepth = 0.4f;
    [SerializeField] private float topSearchStep = 0.05f;
    [SerializeField] private float topSearchExtraHeight = 0.2f;
    [SerializeField] private float maximumTopSurfaceAngle = 45f;

    [Header("Collision")]
    [SerializeField] private LayerMask traversalMask = Physics.DefaultRaycastLayers;

    private readonly RaycastHit[] obstacleHits = new RaycastHit[HitCapacity];
    private readonly RaycastHit[] topHits = new RaycastHit[HitCapacity];

    private CharacterController characterController;

    public bool HasCandidate { get; private set; }
    public TraversalCandidate CurrentCandidate { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HasCandidate = TryDetect(out TraversalCandidate candidate);

        if (HasCandidate)
            CurrentCandidate = candidate;
    }

    public bool TryDetect(out TraversalCandidate candidate)
    {
        candidate = default;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 obstacleOrigin = transform.position + Vector3.up * obstacleProbeHeight;

        if (!TryFindObstacle(obstacleOrigin, forward, out RaycastHit obstacleHit))
            return false;

        if (!TryFindTop(obstacleHit, out RaycastHit topHit))
            return false;

        float height = topHit.point.y - transform.position.y;

        if (height < minimumTraversalHeight || height > maximumTraversalHeight)
            return false;

        float topAngle = Vector3.Angle(topHit.normal, Vector3.up);

        if (topAngle > maximumTopSurfaceAngle)
            return false;

        candidate = new TraversalCandidate(
            obstacleHit.collider,
            obstacleHit.point,
            obstacleHit.normal,
            topHit.point,
            topHit.normal,
            height
        );

        return true;
    }

    private bool TryFindObstacle(Vector3 origin, Vector3 direction, out RaycastHit bestHit)
    {
        bestHit = default;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            obstacleProbeRadius,
            direction,
            obstacleHits,
            obstacleProbeDistance,
            traversalMask,
            QueryTriggerInteraction.Ignore
        );

        bool foundHit = false;
        float nearestDistance = float.MaxValue;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = obstacleHits[index];

            if (!IsValidExternalCollider(hit.collider))
                continue;

            if (hit.distance >= nearestDistance)
                continue;

            foundHit = true;
            nearestDistance = hit.distance;
            bestHit = hit;
        }

        return foundHit;
    }

    private bool TryFindTop(RaycastHit obstacleHit, out RaycastHit bestHit)
    {
        bestHit = default;

        for (float depth = topSearchStartDepth; depth <= topSearchMaximumDepth; depth += topSearchStep)
        {
            Vector3 searchPoint = obstacleHit.point - obstacleHit.normal * depth;
            Vector3 origin = searchPoint + Vector3.up * (maximumTraversalHeight + topSearchExtraHeight);
            float distance = maximumTraversalHeight + topSearchExtraHeight;

            int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, topHits, distance, traversalMask, QueryTriggerInteraction.Ignore);

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = topHits[index];

                if (!IsValidExternalCollider(hit.collider))
                    continue;

                if (hit.collider != obstacleHit.collider)
                    continue;

                float topAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (topAngle > maximumTopSurfaceAngle)
                    continue;

                bestHit = hit;
                return true;
            }
        }

        return false;
    }

    private bool IsValidExternalCollider(Collider collider)
    {
        if (collider == null || collider == characterController)
            return false;

        return !collider.transform.IsChildOf(transform);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 obstacleOrigin = transform.position + Vector3.up * obstacleProbeHeight;
        Vector3 obstacleEnd = obstacleOrigin + forward * obstacleProbeDistance;

        Gizmos.DrawWireSphere(obstacleOrigin, obstacleProbeRadius);
        Gizmos.DrawWireSphere(obstacleEnd, obstacleProbeRadius);
        Gizmos.DrawLine(obstacleOrigin, obstacleEnd);

        if (!HasCandidate)
            return;

        Gizmos.DrawWireSphere(CurrentCandidate.ObstaclePoint, obstacleProbeRadius);
        Gizmos.DrawLine(CurrentCandidate.ObstaclePoint, CurrentCandidate.ObstaclePoint + CurrentCandidate.ObstacleNormal * 0.4f);

        Gizmos.DrawWireSphere(CurrentCandidate.TopPoint, obstacleProbeRadius);
        Gizmos.DrawLine(CurrentCandidate.TopPoint, CurrentCandidate.TopPoint + CurrentCandidate.TopNormal * 0.4f);
    }
}