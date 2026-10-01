using UnityEngine;

public class MemberHudMenuController : MonoBehaviour
{
    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudSlot[] slots;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            MemberDefinition member = memberLoadout.GetMember(i);

            if (member != null)
                slots[i].SetMember(member);
            else
                slots[i].SetEmpty();
        }
    }

    public void ShowMemberLog(MemberDefinition member, string message)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (memberLoadout.GetMember(i) != member)
                continue;

            slots[i].ShowLog($"{member.Code} // {message}");
            return;
        }
    }
}