using System;
using UnityEngine;

public class MemberAssembly : MonoBehaviour
{
    private const int TotalSlots = 8;

    [SerializeField, Range(0, TotalSlots)]
    private int unlockedSlotCount = 2;

    [SerializeField]
    private MemberDefinition[] installedMembers = new MemberDefinition[TotalSlots];

    public int SlotCount => TotalSlots;
    public int UnlockedSlotCount => unlockedSlotCount;

    public bool IsSlotUnlocked(int index)
    {
        return index >= 0 && index < unlockedSlotCount;
    }

    public MemberDefinition GetMember(int index)
    {
        if (index < 0 || index >= installedMembers.Length)
            return null;

        return installedMembers[index];
    }

    public bool IsMemberStartSlot(int index)
    {
        MemberDefinition member = GetMember(index);

        if (member == null)
            return false;

        // Multi-slot Members are stored in every occupied slot.
        // Only the first slot represents the Member in the UI.
        return index == 0 || installedMembers[index - 1] != member;
    }

    public bool HasMember(MemberDefinition member)
    {
        if (member == null)
            return false;

        foreach (MemberDefinition installedMember in installedMembers)
        {
            if (installedMember == member)
                return true;
        }

        return false;
    }

    public MemberDefinition GetFirstMemberByClass(MemberClass memberClass)
    {
        foreach (MemberDefinition member in installedMembers)
        {
            if (member != null && member.Class == memberClass)
                return member;
        }

        return null;
    }

    public bool TryInstallMember(int startIndex, MemberDefinition member)
    {
        if (member == null)
            return false;

        int endIndex = startIndex + member.SlotCost;

        if (startIndex < 0 || endIndex > installedMembers.Length)
            return false;

        for (int i = startIndex; i < endIndex; i++)
        {
            if (!IsSlotUnlocked(i))
                return false;

            // Allow overlap with the Member's current position when moving it.
            if (installedMembers[i] != null && installedMembers[i] != member)
                return false;
        }

        RemoveMemberInternal(member);

        for (int i = startIndex; i < endIndex; i++)
            installedMembers[i] = member;

        return true;
    }

    public bool TryRemoveMemberAt(int index)
    {
        MemberDefinition member = GetMember(index);

        if (member == null)
            return false;

        RemoveMemberInternal(member);
        return true;
    }

    public void SetUnlockedSlotCount(int count)
    {
        unlockedSlotCount = Mathf.Clamp(count, 0, TotalSlots);
    }

    private void RemoveMemberInternal(MemberDefinition member)
    {
        for (int i = 0; i < installedMembers.Length; i++)
        {
            if (installedMembers[i] == member)
                installedMembers[i] = null;
        }
    }

    private void OnValidate()
    {
        unlockedSlotCount = Mathf.Clamp(unlockedSlotCount, 0, TotalSlots);

        if (installedMembers != null && installedMembers.Length == TotalSlots)
            return;

        MemberDefinition[] resizedMembers = new MemberDefinition[TotalSlots];

        if (installedMembers != null)
        {
            Array.Copy(
                installedMembers,
                resizedMembers,
                Mathf.Min(installedMembers.Length, TotalSlots)
            );
        }

        installedMembers = resizedMembers;
    }
}