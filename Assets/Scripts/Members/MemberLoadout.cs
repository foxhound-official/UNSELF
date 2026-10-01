using UnityEngine;

public class MemberLoadout : MonoBehaviour
{
    [SerializeField] private MemberDefinition[] activeMembers = new MemberDefinition[3];

    public bool HasMemberClass(MemberClass memberClass)
    {
        foreach (MemberDefinition member in activeMembers)
        {
            if (member != null && member.Class == memberClass)
                return true;
        }

        return false;
    }
}