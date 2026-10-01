using UnityEngine;

public enum MemberClass
{
    Social,
    Technical,
    Combat
}

[CreateAssetMenu(fileName = "Member", menuName = "UNSELF/Members/Member")]
public class MemberDefinition : ScriptableObject
{
    [SerializeField] private string code;
    [SerializeField] private string memberName;
    [SerializeField] private MemberClass memberClass;

    [Range(1, 3)]
    [SerializeField] private int slotCost = 1;

    public string Code => code;
    public string MemberName => memberName;
    public MemberClass Class => memberClass;
    public int SlotCost => slotCost;
}