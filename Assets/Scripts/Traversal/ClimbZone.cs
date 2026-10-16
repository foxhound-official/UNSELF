using UnityEngine;

public class ClimbZone : MonoBehaviour
{
    [SerializeField] private Transform hangPoint;
    [SerializeField] private Transform standPoint;
    [SerializeField] private MemberCapability requiredCapability;

    [Header("Approach")]
    [SerializeField, Range(-1f, 1f)]
    private float minimumApproachDot = 0.35f;

    [SerializeField, Range(-1f, 1f)]
    private float minimumVelocityDot = 0.5f;

    public Transform HangPoint => hangPoint;
    public Transform StandPoint => standPoint;
    public MemberCapability RequiredCapability => requiredCapability;

    public float MinimumApproachDot => minimumApproachDot;
    public float MinimumVelocityDot => minimumVelocityDot;

    public Vector3 ApproachDirection => transform.forward;
}