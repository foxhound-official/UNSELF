using UnityEngine;

public class MemberLoadout : MonoBehaviour
{
    [SerializeField] private MemberDefinition[] activeMembers = new MemberDefinition[3];

    public int SlotCount => activeMembers.Length;

    public MemberDefinition GetMember(int index)
    {
        if (index < 0 || index >= activeMembers.Length)
            return null;

        return activeMembers[index];
    }

    public bool HasMemberClass(MemberClass memberClass)
    {
        foreach (MemberDefinition member in activeMembers)
        {
            if (member != null && member.Class == memberClass)
                return true;
        }

        return false;
    }

    public MemberDefinition GetFirstMemberByClass(MemberClass memberClass)
    {
        foreach (MemberDefinition member in activeMembers)
        {
            if (member != null && member.Class == memberClass)
                return member;
        }

        return null;
    }

    public bool TrySetMember(int index, MemberDefinition member)
    {
        if (index < 0 || index >= activeMembers.Length || member == null)
            return false;

        for (int i = 0; i < activeMembers.Length; i++)
        {
            if (activeMembers[i] == member)
                activeMembers[i] = null;
        }

        activeMembers[index] = member;
        return true;
    }
}