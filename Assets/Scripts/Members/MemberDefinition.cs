using UnityEngine;
using System.Collections.Generic;

public enum MemberClass
{
    Social,
    Technical,
    Combat,
    Agility
}

[CreateAssetMenu(fileName = "Member", menuName = "UNSELF/Members/Member")]
public class MemberDefinition : ScriptableObject
{
    [SerializeField] private string code;
    [SerializeField] private string memberName;
    [SerializeField] private MemberClass memberClass;

    [Range(1, 3)]
    [SerializeField] private int slotCost = 1;

    [SerializeField] private MemberCapability[] capabilities;
    
    [Header("Description")]
    [SerializeField, TextArea(2, 4)]
    private string shortDescription;

    [SerializeField, TextArea(4, 8)]
    private string personalityDescription;

    [Header("Recovered Memory")]
    [SerializeField, TextArea(5, 12)]
    private string memoryRecord;
    [SerializeField]
    private MemberMemoryFragment[] memoryFragments = System.Array.Empty<MemberMemoryFragment>();

    [Header("Gameplay")]
    [SerializeField, TextArea(3, 7)]
    private string gameplayEffects;

    [SerializeField, TextArea(3, 7)]
    private string sideEffects;

    [Header("Integrity")]
    [SerializeField, TextArea(3, 7)]
    private string integrityNotes;

    [Header("Origin")]
    [SerializeField, TextArea(3, 8)]
    private string originNotes;

    public string Code => code;
    public string MemberName => memberName;
    public MemberClass Class => memberClass;
    public int SlotCost => slotCost;
    
    public string ShortDescription => shortDescription;
    public string PersonalityDescription => personalityDescription;
    public string MemoryRecord => memoryRecord;
    public string GameplayEffects => gameplayEffects;
    public string SideEffects => sideEffects;
    public string IntegrityNotes => integrityNotes;
    public string OriginNotes => originNotes;

    public IReadOnlyList<MemberMemoryFragment> MemoryFragments => memoryFragments ?? System.Array.Empty<MemberMemoryFragment>();
    public IReadOnlyList<MemberCapability> Capabilities => capabilities ?? System.Array.Empty<MemberCapability>();

    public bool HasCapability(MemberCapability capability)
    {
        if (capability == null || capabilities == null)
            return false;

        foreach (MemberCapability memberCapability in capabilities)
        {
            if (memberCapability == capability)
                return true;
        }

        return false;
    }

    public bool HasCapability(string capabilityCode)
    {
        if (string.IsNullOrWhiteSpace(capabilityCode) || capabilities == null)
            return false;

        foreach (MemberCapability capability in capabilities)
        {
            if (capability != null && capability.Code == capabilityCode)
                return true;
        }

        return false;
    }
}