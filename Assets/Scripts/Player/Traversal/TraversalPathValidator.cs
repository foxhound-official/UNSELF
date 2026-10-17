using UnityEngine;

public class TraversalPathValidator : MonoBehaviour
{
    private const int OverlapCapacity = 16;

    [SerializeField] private float sampleDistance = 0.1f;
    [SerializeField] private LayerMask traversalMask = Physics.DefaultRaycastLayers;

    private readonly Collider[] overlaps = new Collider[OverlapCapacity];

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
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

    public bool IsQuadraticBezierClear(Vector3 start, Vector3 control, Vector3 end, Collider ignoredCollider)
    {
        float estimatedDistance = Vector3.Distance(start, control) + Vector3.Distance(control, end);
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(estimatedDistance / sampleDistance));

        for (int index = 0; index <= sampleCount; index++)
        {
            float progress = (float)index / sampleCount;
            Vector3 position = EvaluateQuadraticBezier(start, control, end, progress);

            if (!IsPositionClear(position, ignoredCollider))
                return false;
        }

        return true;
    }

    private bool IsPositionClear(Vector3 rootPosition, Collider ignoredCollider)
    {
        float skinWidth = characterController.skinWidth;
        float radius = Mathf.Max(0.01f, characterController.radius - skinWidth);
        float halfHeight = Mathf.Max(radius, characterController.height * 0.5f - skinWidth);

        Vector3 center = rootPosition + transform.rotation * characterController.center;
        Vector3 bottom = center - transform.up * (halfHeight - radius);
        Vector3 top = center + transform.up * (halfHeight - radius);

        int overlapCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, traversalMask, QueryTriggerInteraction.Ignore);

        for (int index = 0; index < overlapCount; index++)
        {
            Collider overlap = overlaps[index];

            if (overlap == null || overlap == characterController || overlap == ignoredCollider)
                continue;

            if (overlap.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    private Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float progress)
    {
        float inverseProgress = 1f - progress;
        return inverseProgress * inverseProgress * start + 2f * inverseProgress * progress * control + progress * progress * end;
    }
}