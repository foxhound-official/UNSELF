using UnityEngine;

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

    public string Code => code;
    public string MemberName => memberName;
    public MemberClass Class => memberClass;
    public int SlotCost => slotCost;

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