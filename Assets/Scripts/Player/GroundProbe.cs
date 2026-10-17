using UnityEngine;

public class GroundProbe : MonoBehaviour
{
    private const int PeripheralProbeCount = 4;

    [SerializeField] private float probeStartHeight = 0.2f;
    [SerializeField] private float probeDistance = 0.35f;
    [SerializeField] private float supportProbeOffset = 0.18f;
    [SerializeField] private int minimumPeripheralSupportHits = 2;
    [SerializeField] private float maximumGroundAngle = 80f;
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;

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
        Vector3 centerOrigin = transform.position + Vector3.up * probeStartHeight;

        if (TryProbe(centerOrigin, out RaycastHit centerHit))
        {
            SetGround(centerHit.point, centerHit.normal);
            return;
        }

        Vector3 right = transform.right * supportProbeOffset;
        Vector3 forward = transform.forward * supportProbeOffset;

        Vector3[] origins =
        {
            centerOrigin + right,
            centerOrigin - right,
            centerOrigin + forward,
            centerOrigin - forward
        };

        int supportHits = 0;
        Vector3 accumulatedPoint = Vector3.zero;
        Vector3 accumulatedNormal = Vector3.zero;

        for (int index = 0; index < PeripheralProbeCount; index++)
        {
            if (!TryProbe(origins[index], out RaycastHit hit))
                continue;

            supportHits++;
            accumulatedPoint += hit.point;
            accumulatedNormal += hit.normal;
        }

        if (supportHits >= minimumPeripheralSupportHits)
        {
            Vector3 groundPoint = accumulatedPoint / supportHits;
            Vector3 groundNormal = accumulatedNormal.normalized;

            SetGround(groundPoint, groundNormal);
            return;
        }

        ClearGround();
    }

    private bool TryProbe(Vector3 origin, out RaycastHit hit)
    {
        if (!Physics.Raycast(origin, Vector3.down, out hit, probeStartHeight + probeDistance, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider == null || hit.collider == characterController)
            return false;

        float angle = Vector3.Angle(hit.normal, Vector3.up);
        return angle <= maximumGroundAngle;
    }

    private void SetGround(Vector3 point, Vector3 normal)
    {
        IsGrounded = true;
        GroundPoint = point;
        GroundNormal = normal;
        GroundAngle = Vector3.Angle(normal, Vector3.up);
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
        Vector3 centerOrigin = transform.position + Vector3.up * probeStartHeight;
        Vector3 right = transform.right * supportProbeOffset;
        Vector3 forward = transform.forward * supportProbeOffset;

        DrawProbe(centerOrigin);
        DrawProbe(centerOrigin + right);
        DrawProbe(centerOrigin - right);
        DrawProbe(centerOrigin + forward);
        DrawProbe(centerOrigin - forward);
    }

    private void DrawProbe(Vector3 origin)
    {
        Gizmos.DrawLine(origin, origin + Vector3.down * (probeStartHeight + probeDistance));
    }
}