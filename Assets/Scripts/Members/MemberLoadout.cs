using System;
using UnityEngine;

public class MemberLoadout : MonoBehaviour
{
    private const int HudSlotCount = 3;

    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberDefinition[] hudMembers = new MemberDefinition[HudSlotCount];

    public int SlotCount => HudSlotCount;

    public MemberDefinition GetMember(int index)
    {
        if (index < 0 || index >= hudMembers.Length)
            return null;

        return hudMembers[index];
    }

    public bool IsMemberVisible(MemberDefinition member)
    {
        if (member == null)
            return false;

        foreach (MemberDefinition hudMember in hudMembers)
        {
            if (hudMember == member)
                return true;
        }

        return false;
    }

    public bool TryToggleMember(MemberDefinition member)
    {
        if (member == null || !memberAssembly.HasMember(member))
            return false;

        // Clicking an already visible Member removes it from the HUD.
        for (int i = 0; i < hudMembers.Length; i++)
        {
            if (hudMembers[i] != member)
                continue;

            hudMembers[i] = null;
            return true;
        }

        // New HUD Members occupy the first available display slot.
        for (int i = 0; i < hudMembers.Length; i++)
        {
            if (hudMembers[i] != null)
                continue;

            hudMembers[i] = member;
            return true;
        }

        return false;
    }

    public void RemoveMember(MemberDefinition member)
    {
        for (int i = 0; i < hudMembers.Length; i++)
        {
            if (hudMembers[i] == member)
                hudMembers[i] = null;
        }
    }

    private void OnValidate()
    {
        if (hudMembers != null && hudMembers.Length == HudSlotCount)
            return;

        MemberDefinition[] resizedMembers = new MemberDefinition[HudSlotCount];

        if (hudMembers != null)
        {
            Array.Copy(
                hudMembers,
                resizedMembers,
                Mathf.Min(hudMembers.Length, HudSlotCount)
            );
        }

        hudMembers = resizedMembers;
    }
}