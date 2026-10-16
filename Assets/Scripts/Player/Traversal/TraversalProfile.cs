using UnityEngine;

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
}