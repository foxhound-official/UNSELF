using KinematicCharacterController;
using UnityEngine;

public class TraversalPathValidator : MonoBehaviour
{
    private const int OverlapCapacity = 16;
    private const float CapsuleClearance = 0.01f;

    [SerializeField] private float sampleDistance = 0.1f;
    [SerializeField] private LayerMask traversalMask = Physics.DefaultRaycastLayers;

    private readonly Collider[] overlaps = new Collider[OverlapCapacity];

    private KinematicCharacterMotor motor;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    public bool IsSegmentClear(Vector3 start, Vector3 end, Collider ignoredCollider)
    {
        float distance = Vector3.Distance(start, end);
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleDistance));

        for (int index = 0; index <= sampleCount; index++)
        {
            float progress = (float)index / sampleCount;
            Vector3 position = Vector3.Lerp(start, end, progress);

            if (!IsPositionClear(position, ignoredCollider))
                return false;
        }

        return true;
    }

    private bool IsPositionClear(Vector3 rootPosition, Collider ignoredCollider)
    {
        if (motor == null || motor.Capsule == null)
            return false;

        float radius = Mathf.Max(0.01f, motor.Capsule.radius - CapsuleClearance);
        float halfHeight = Mathf.Max(radius, motor.Capsule.height * 0.5f - CapsuleClearance);

        Vector3 center = rootPosition + transform.rotation * motor.Capsule.center;
        Vector3 bottom = center - transform.up * (halfHeight - radius);
        Vector3 top = center + transform.up * (halfHeight - radius);

        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            overlaps,
            traversalMask,
            QueryTriggerInteraction.Ignore
        );

        for (int index = 0; index < overlapCount; index++)
        {
            Collider overlap = overlaps[index];

            if (overlap == null)
                continue;

            if (overlap == motor.Capsule || overlap == ignoredCollider)
                continue;

            if (overlap.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }
}