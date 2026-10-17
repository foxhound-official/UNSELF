using KinematicCharacterController;
using UnityEngine;

public struct LedgeGrabCandidate
{
    public Collider Collider;
    public Vector3 WallPoint;
    public Vector3 WallNormal;
    public Vector3 TopPoint;
    public Vector3 TopNormal;

    public LedgeGrabCandidate(
        Collider collider,
        Vector3 wallPoint,
        Vector3 wallNormal,
        Vector3 topPoint,
        Vector3 topNormal)
    {
        Collider = collider;
        WallPoint = wallPoint;
        WallNormal = wallNormal;
        TopPoint = topPoint;
        TopNormal = topNormal;
    }
}

public class LedgeGrabDetector : MonoBehaviour
{
    private const int HitCapacity = 8;
    private const float HeightComparisonTolerance = 0.001f;

    [Header("Wall Detection")]
    [SerializeField] private float minimumProbeHeight = 0.65f;
    [SerializeField] private float maximumProbeHeight = 2.4f;
    [SerializeField] private float probeHeightStep = 0.15f;
    [SerializeField] private float wallProbeRadius = 0.12f;
    [SerializeField] private float wallProbeDistance = 1.15f;

    [Header("Top Detection")]
    [SerializeField] private float topProbeForwardOffset = 0.2f;
    [SerializeField] private float topProbeStartHeight = 0.6f;
    [SerializeField] private float topProbeDistance = 1.2f;
    [SerializeField] private float maximumTopSurfaceAngle = 45f;

    [Header("Grab Limits")]
    [SerializeField] private float minimumLedgeHeight = 0.45f;
    [SerializeField] private float maximumLedgeHeight = 2.55f;
    [SerializeField] private float maximumWallAngle = 50f;

    [Header("Collision")]
    [SerializeField] private LayerMask traversalMask = Physics.DefaultRaycastLayers;

    private readonly RaycastHit[] wallHits = new RaycastHit[HitCapacity];
    private readonly RaycastHit[] topHits = new RaycastHit[HitCapacity];

    private KinematicCharacterMotor motor;

    public bool HasCandidate { get; private set; }
    public LedgeGrabCandidate CurrentCandidate { get; private set; }

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    private void Update()
    {
        HasCandidate = TryDetect(out LedgeGrabCandidate candidate);

        if (HasCandidate)
            CurrentCandidate = candidate;
    }

    public bool TryDetect(out LedgeGrabCandidate candidate)
    {
        candidate = default;

        if (motor == null)
            return false;

        Vector3 up = motor.CharacterUp;
        Vector3 forward = Vector3.ProjectOnPlane(motor.CharacterForward, up).normalized;

        if (forward.sqrMagnitude <= 0.0001f)
            return false;

        if (!TryFindWall(forward, up, out RaycastHit wallHit))
            return false;

        if (!TryFindTop(wallHit, forward, up, out RaycastHit topHit))
            return false;

        float ledgeHeight = Vector3.Dot(topHit.point - motor.TransientPosition, up);

        if (ledgeHeight < minimumLedgeHeight || ledgeHeight > maximumLedgeHeight)
            return false;

        candidate = new LedgeGrabCandidate(
            wallHit.collider,
            wallHit.point,
            wallHit.normal,
            topHit.point,
            topHit.normal
        );

        return true;
    }

    private bool TryFindWall(Vector3 forward, Vector3 up, out RaycastHit bestHit)
    {
        bestHit = default;

        bool foundHit = false;
        float highestHit = float.MinValue;
        float nearestDistanceAtHeight = float.MaxValue;

        // The highest valid wall hit is closest to the ledge that the player can actually grab.
        for (float height = minimumProbeHeight;
             height <= maximumProbeHeight + HeightComparisonTolerance;
             height += probeHeightStep)
        {
            Vector3 origin = motor.TransientPosition + up * height;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                wallProbeRadius,
                forward,
                wallHits,
                wallProbeDistance,
                traversalMask,
                QueryTriggerInteraction.Ignore
            );

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = wallHits[index];

                if (!IsValidExternalCollider(hit.collider))
                    continue;

                Vector3 horizontalNormal = Vector3.ProjectOnPlane(hit.normal, up).normalized;

                if (horizontalNormal.sqrMagnitude <= 0.0001f)
                    continue;

                float wallAngle = Vector3.Angle(-forward, horizontalNormal);

                if (wallAngle > maximumWallAngle)
                    continue;

                float hitHeight = Vector3.Dot(hit.point - motor.TransientPosition, up);

                bool isHigher = hitHeight > highestHit + HeightComparisonTolerance;
                bool isSameHeightButCloser =
                    Mathf.Abs(hitHeight - highestHit) <= HeightComparisonTolerance &&
                    hit.distance < nearestDistanceAtHeight;

                if (!isHigher && !isSameHeightButCloser)
                    continue;

                foundHit = true;
                highestHit = hitHeight;
                nearestDistanceAtHeight = hit.distance;
                bestHit = hit;
            }
        }

        return foundHit;
    }

    private bool TryFindTop(
        RaycastHit wallHit,
        Vector3 forward,
        Vector3 up,
        out RaycastHit bestHit)
    {
        bestHit = default;

        Vector3 origin =
            wallHit.point +
            forward * topProbeForwardOffset +
            up * topProbeStartHeight;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            -up,
            topHits,
            topProbeDistance,
            traversalMask,
            QueryTriggerInteraction.Ignore
        );

        bool foundHit = false;
        float nearestDistance = float.MaxValue;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = topHits[index];

            if (!IsValidExternalCollider(hit.collider))
                continue;

            if (hit.collider != wallHit.collider)
                continue;

            float topAngle = Vector3.Angle(hit.normal, up);

            if (topAngle > maximumTopSurfaceAngle)
                continue;

            if (hit.distance >= nearestDistance)
                continue;

            foundHit = true;
            nearestDistance = hit.distance;
            bestHit = hit;
        }

        return foundHit;
    }

    private bool IsValidExternalCollider(Collider collider)
    {
        if (collider == null)
            return false;

        if (collider == motor.Capsule)
            return false;

        return !collider.transform.IsChildOf(transform);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 up = Application.isPlaying && motor != null ? motor.CharacterUp : transform.up;

        Vector3 forward = Application.isPlaying && motor != null
            ? Vector3.ProjectOnPlane(motor.CharacterForward, up).normalized
            : Vector3.ProjectOnPlane(transform.forward, up).normalized;

        Vector3 rootPosition = Application.isPlaying && motor != null
            ? motor.TransientPosition
            : transform.position;

        for (float height = minimumProbeHeight;
             height <= maximumProbeHeight + HeightComparisonTolerance;
             height += probeHeightStep)
        {
            Vector3 origin = rootPosition + up * height;
            Vector3 end = origin + forward * wallProbeDistance;

            Gizmos.DrawWireSphere(origin, wallProbeRadius);
            Gizmos.DrawWireSphere(end, wallProbeRadius);
            Gizmos.DrawLine(origin, end);
        }

        if (!HasCandidate)
            return;

        Gizmos.DrawWireSphere(CurrentCandidate.WallPoint, 0.08f);
        Gizmos.DrawLine(
            CurrentCandidate.WallPoint,
            CurrentCandidate.WallPoint + CurrentCandidate.WallNormal * 0.4f
        );

        Gizmos.DrawWireSphere(CurrentCandidate.TopPoint, 0.1f);
        Gizmos.DrawLine(
            CurrentCandidate.TopPoint,
            CurrentCandidate.TopPoint + CurrentCandidate.TopNormal * 0.4f
        );
    }
}