using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TraversalProfile", menuName = "UNSELF/Player/Traversal Profile")]
public class TraversalProfile : ScriptableObject
{
    [Header("Vault")]
    [SerializeField] private float vaultMinimumDuration = 0.3f;
    [SerializeField] private float vaultMaximumDuration = 0.55f;
    [SerializeField] private float vaultMinimumTraversalSpeed = 3.5f;
    [SerializeField] private float vaultEntryClearance = 0.05f;
    [SerializeField] private float vaultEntryRise = 0.08f;
    [SerializeField] private float vaultTopClearance = 0.2f;
    [SerializeField] private float vaultForwardClearance = 0.15f;
    [SerializeField] private AnimationCurve vaultProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float vaultExitSpeedBoost = 0.75f;
    [SerializeField] private float enhancedVaultExitSpeedBoost = 1.5f;
    [SerializeField, Min(0.01f)] private float vaultExitBoostRiseTime = 0.18f;
    [SerializeField, Min(0.01f)] private float vaultExitBoostFallTime = 0.35f;

    [Header("Ledge Climb")]
    [FormerlySerializedAs("ledgeGrabMaximumUpwardSpeed")]
    [SerializeField] private float ledgeClimbMaximumUpwardSpeed = 9f;

    [FormerlySerializedAs("ledgeGrabWallClearance")]
    [SerializeField] private float ledgeClimbWallClearance = 0.05f;

    [FormerlySerializedAs("ledgeGrabHangDepth")]
    [SerializeField] private float ledgeClimbHangDepth = 1.9f;

    [FormerlySerializedAs("ledgeGrabTopClearance")]
    [SerializeField] private float ledgeClimbTopClearance = 0.12f;

    [FormerlySerializedAs("ledgeGrabCrestForward")]
    [SerializeField] private float ledgeClimbCrestForward = 0.25f;

    [FormerlySerializedAs("ledgeGrabLandingForward")]
    [SerializeField] private float ledgeClimbLandingForward = 0.45f;

    [FormerlySerializedAs("ledgeGrabCatchDuration")]
    [SerializeField] private float ledgeClimbCatchDuration = 0.12f;

    [FormerlySerializedAs("ledgeGrabPullDuration")]
    [SerializeField] private float ledgeClimbPullDuration = 0.42f;

    [FormerlySerializedAs("ledgeGrabExitSpeedMultiplier")]
    [SerializeField] private float ledgeClimbExitSpeedMultiplier = 0.5f;

    [FormerlySerializedAs("ledgeGrabCooldown")]
    [SerializeField] private float ledgeClimbCooldown = 0.3f;

    [FormerlySerializedAs("ledgeGrabProgress")]
    [SerializeField] private AnimationCurve ledgeClimbProgress = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float VaultMinimumDuration => vaultMinimumDuration;
    public float VaultMaximumDuration => vaultMaximumDuration;
    public float VaultMinimumTraversalSpeed => vaultMinimumTraversalSpeed;
    public float VaultEntryClearance => vaultEntryClearance;
    public float VaultEntryRise => vaultEntryRise;
    public float VaultTopClearance => vaultTopClearance;
    public float VaultForwardClearance => vaultForwardClearance;
    public AnimationCurve VaultProgress => vaultProgress;
    public float VaultExitSpeedBoost => vaultExitSpeedBoost;
    public float EnhancedVaultExitSpeedBoost => enhancedVaultExitSpeedBoost;
    public float VaultExitBoostRiseTime => vaultExitBoostRiseTime;
    public float VaultExitBoostFallTime => vaultExitBoostFallTime;
    

    public float LedgeClimbMaximumUpwardSpeed => ledgeClimbMaximumUpwardSpeed;
    public float LedgeClimbWallClearance => ledgeClimbWallClearance;
    public float LedgeClimbHangDepth => ledgeClimbHangDepth;
    public float LedgeClimbTopClearance => ledgeClimbTopClearance;
    public float LedgeClimbCrestForward => ledgeClimbCrestForward;
    public float LedgeClimbLandingForward => ledgeClimbLandingForward;
    public float LedgeClimbCatchDuration => ledgeClimbCatchDuration;
    public float LedgeClimbPullDuration => ledgeClimbPullDuration;
    public float LedgeClimbExitSpeedMultiplier => ledgeClimbExitSpeedMultiplier;
    public float LedgeClimbCooldown => ledgeClimbCooldown;
    public AnimationCurve LedgeClimbProgress => ledgeClimbProgress;
}