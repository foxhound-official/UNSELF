using UnityEngine;

public class GroundProbe : MonoBehaviour
{
    private const int HitCapacity = 8;

    [SerializeField] private float probeStartHeight = 0.2f;
    [SerializeField] private float probeDistance = 0.35f;
    [SerializeField] private float probeRadius = 0.22f;
    [SerializeField] private float maximumGroundAngle = 80f;
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;

    private readonly RaycastHit[] hits = new RaycastHit[HitCapacity];

    private CharacterController characterController;

    public bool IsGrounded { get; private set; }
    public Vector3 GroundPoint { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public float GroundAngle { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Probe()
    {
        Vector3 origin = transform.position + Vector3.up * probeStartHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit centerHit, probeStartHeight + probeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (IsValidGround(centerHit))
            {
                SetGround(centerHit);
                return;
            }
        }

        Vector3 sphereOrigin = transform.position + Vector3.up * probeRadius;
        int hitCount = Physics.SphereCastNonAlloc(sphereOrigin, probeRadius, Vector3.down, hits, probeDistance, groundMask, QueryTriggerInteraction.Ignore);

        bool foundGround = false;
        RaycastHit bestHit = default;
        float bestUpAlignment = -1f;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = hits[index];

            if (!IsValidGround(hit))
                continue;

            float upAlignment = Vector3.Dot(hit.normal, Vector3.up);

            if (upAlignment <= bestUpAlignment)
                continue;

            foundGround = true;
            bestHit = hit;
            bestUpAlignment = upAlignment;
        }

        if (foundGround)
        {
            SetGround(bestHit);
            return;
        }

        ClearGround();
    }

    private bool IsValidGround(RaycastHit hit)
    {
        if (hit.collider == null || hit.collider == characterController)
            return false;

        float angle = Vector3.Angle(hit.normal, Vector3.up);
        return angle <= maximumGroundAngle;
    }

    private void SetGround(RaycastHit hit)
    {
        IsGrounded = true;
        GroundPoint = hit.point;
        GroundNormal = hit.normal;
        GroundAngle = Vector3.Angle(hit.normal, Vector3.up);
    }

    private void ClearGround()
    {
        IsGrounded = false;
        GroundPoint = transform.position;
        GroundNormal = Vector3.up;
        GroundAngle = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * probeStartHeight;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * (probeStartHeight + probeDistance));

        Vector3 sphereOrigin = transform.position + Vector3.up * probeRadius;
        Gizmos.DrawWireSphere(sphereOrigin, probeRadius);
        Gizmos.DrawWireSphere(sphereOrigin + Vector3.down * probeDistance, probeRadius);
    }
}