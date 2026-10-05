using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MemberMenuController : MonoBehaviour
{
    private enum MemberMenuMode
    {
        View,
        EditAssembly
    }

    [SerializeField] private GameObject menuRoot;
    [SerializeField] private MemberMenuTransition menuTransition;

    [Header("Player Data")]
    [SerializeField] private MemberInventory memberInventory;
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudMenuController memberHud;

    [Header("Player Control")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Member Archive")]
    [SerializeField] private Transform memberListContent;
    [SerializeField] private Button memberEntryTemplate;

    [Header("Member Assembly")]
    [SerializeField] private Transform assemblyList;
    [SerializeField] private AssemblySlotUI assemblySlotTemplate;

    private readonly List<AssemblySlotUI> assemblySlotViews = new();

    private MemberDefinition selectedMember;
    private MemberMenuMode currentMode = MemberMenuMode.View;

    private bool isOpen;
    private bool isClosing;

    private bool movementWasEnabled;
    private bool lookWasEnabled;
    private bool interactionWasEnabled;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private bool CanEditAssembly =>
        currentMode == MemberMenuMode.EditAssembly;

    private void Start()
    {
        CreateAssemblySlots();

        menuRoot.SetActive(false);
        Refresh();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleMenu();
            return;
        }

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseMenu();
    }

    public void ToggleMenu()
    {
        if (isClosing)
            return;

        if (isOpen)
        {
            CloseMenu();
            return;
        }

        OpenMenu(MemberMenuMode.View);
    }

    public void OpenAssemblyEditor()
    {
        if (isOpen || isClosing)
            return;

        OpenMenu(MemberMenuMode.EditAssembly);
    }

    public void InstallSelectedMember(int slotIndex)
    {
        if (!CanEditAssembly || selectedMember == null)
            return;

        if (!memberAssembly.TryInstallMember(slotIndex, selectedMember))
            return;

        selectedMember = null;
        Refresh();
    }

    public void RemoveMember(int slotIndex)
    {
        if (!CanEditAssembly)
            return;

        MemberDefinition member = memberAssembly.GetMember(slotIndex);

        if (member == null)
            return;

        if (!memberAssembly.TryRemoveMemberAt(slotIndex))
            return;

        // A Member removed from Y.O.U. cannot remain exposed in the HUD.
        memberLoadout.RemoveMember(member);

        memberHud.Refresh();
        Refresh();
    }

    public void ToggleHudMember(int slotIndex)
    {
        MemberDefinition member = memberAssembly.GetMember(slotIndex);

        if (member == null)
            return;

        if (!memberLoadout.TryToggleMember(member))
            return;

        memberHud.Refresh();
        Refresh();
    }

    private void OpenMenu(MemberMenuMode mode)
    {
        isOpen = true;
        currentMode = mode;
        selectedMember = null;

        // Preserve the current gameplay state instead of assuming every
        // player system was enabled before the menu opened.
        movementWasEnabled = playerMovement.enabled;
        lookWasEnabled = playerLook.enabled;
        interactionWasEnabled = playerInteraction.enabled;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        playerMovement.enabled = false;
        playerLook.enabled = false;
        playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menuRoot.SetActive(true);
        Refresh();

        menuTransition.PlayOpen();
    }

private void CloseMenu()
{
    if (isClosing)
        return;

    isClosing = true;

    menuTransition.PlayClose(
        CompleteClose
    );
}

    private void CompleteClose()
    {
        isOpen = false;
        isClosing = false;

        menuRoot.SetActive(false);

        selectedMember = null;
        currentMode = MemberMenuMode.View;

        // Restore exactly the gameplay state that existed before opening.
        playerMovement.enabled = movementWasEnabled;
        playerLook.enabled = lookWasEnabled;
        playerInteraction.enabled = interactionWasEnabled;

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void CreateAssemblySlots()
    {
        assemblySlotViews.Clear();

        for (int i = 0; i < memberAssembly.SlotCount; i++)
        {
            AssemblySlotUI slotView =
                Instantiate(assemblySlotTemplate, assemblyList);

            slotView.gameObject.SetActive(true);
            slotView.Initialize(i, this);

            assemblySlotViews.Add(slotView);
        }
    }

    private void Refresh()
    {
        RefreshMemberInventory();

        foreach (AssemblySlotUI slotView in assemblySlotViews)
        {
            slotView.Refresh(
                memberAssembly,
                memberLoadout,
                CanEditAssembly
            );
        }
    }

    private void RefreshMemberInventory()
    {
        // Runtime entries are rebuilt because the archive can change
        // when new Members are acquired during gameplay.
        for (int i = memberListContent.childCount - 1; i >= 0; i--)
        {
            Transform child = memberListContent.GetChild(i);

            if (child.gameObject == memberEntryTemplate.gameObject)
                continue;

            Destroy(child.gameObject);
        }

        foreach (MemberDefinition member in memberInventory.Members)
        {
            Button entry = Instantiate(memberEntryTemplate, memberListContent);

            entry.gameObject.SetActive(true);
            entry.interactable = CanEditAssembly;

            bool isSelected = member == selectedMember;

            MemberArchiveEntryUI entryView = entry.GetComponent<MemberArchiveEntryUI>();

            entryView.Refresh(member, isSelected);

            MemberDefinition capturedMember = member;

            entry.onClick.AddListener(() => SelectMember(capturedMember));
        }
    }

    private void SelectMember(MemberDefinition member)
    {
        if (!CanEditAssembly)
            return;

        selectedMember = member;

        Debug.Log($"MEMBER SELECTED: {member.Code}");

        // Rebuild only the archive so the selection marker updates immediately.
        RefreshMemberInventory();
    }
}