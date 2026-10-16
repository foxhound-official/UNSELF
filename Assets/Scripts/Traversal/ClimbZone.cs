using UnityEngine;

public class ClimbZone : MonoBehaviour
{
    [Header("Traversal")]
    [SerializeField] private Transform entryHangPoint;
    [SerializeField] private Transform shimmyStart;
    [SerializeField] private Transform shimmyEnd;
    [SerializeField] private PullUpPoint[] pullUpPoints;

    [Header("Requirements")]
    [SerializeField] private MemberCapability requiredCapability;

    [Header("Approach")]
    [SerializeField, Range(-1f, 1f)]
    private float minimumApproachDot = 0.35f;

    [SerializeField, Range(-1f, 1f)]
    private float minimumVelocityDot = 0.5f;

    public Transform EntryHangPoint => entryHangPoint;
    public Transform ShimmyStart => shimmyStart;
    public Transform ShimmyEnd => shimmyEnd;

    public MemberCapability RequiredCapability => requiredCapability;

    public float MinimumApproachDot => minimumApproachDot;
    public float MinimumVelocityDot => minimumVelocityDot;

    public Vector3 ApproachDirection => transform.forward;

    public bool TryGetNearestPullUpPoint(
        Vector3 position,
        out PullUpPoint pullUpPoint
    )
    {
        pullUpPoint = null;

        if (pullUpPoints == null)
            return false;

        float nearestDistance = float.MaxValue;

        foreach (PullUpPoint candidate in pullUpPoints)
        {
            if (candidate == null)
                continue;

            if (candidate.HangPoint == null || candidate.StandPoint == null)
                continue;

            if (!candidate.IsInRange(position))
                continue;

            float distance = candidate.GetDistance(position);

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            pullUpPoint = candidate;
        }

        return pullUpPoint != null;
    }
}