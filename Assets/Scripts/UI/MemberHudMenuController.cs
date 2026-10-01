using UnityEngine;

public class MemberHudController : MonoBehaviour
{
    [SerializeField] private MemberHudSlot[] slots;
    [SerializeField] private MemberDefinition[] activeMembers;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < activeMembers.Length && activeMembers[i] != null)
                slots[i].SetMember(activeMembers[i]);
            else
                slots[i].SetEmpty();
        }
    }
}