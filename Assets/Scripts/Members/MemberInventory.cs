using System.Collections.Generic;
using UnityEngine;

public class MemberInventory : MonoBehaviour
{
    [SerializeField] private List<MemberDefinition> members = new();

    public IReadOnlyList<MemberDefinition> Members => members;

    public bool TryAddMember(MemberDefinition member)
    {
        if (member == null || members.Contains(member))
            return false;

        members.Add(member);
        return true;
    }

    public bool HasMember(MemberDefinition member)
    {
        return members.Contains(member);
    }
}