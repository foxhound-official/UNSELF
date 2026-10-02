using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MemberMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;

    [Header("Player Data")]
    [SerializeField] private MemberInventory memberInventory;
    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudMenuController memberHud;

    [Header("Available Members")]
    [SerializeField] private Transform memberListContent;
    [SerializeField] private Button memberEntryTemplate;

    [Header("Active Slots")]
    [SerializeField] private TMP_Text[] activeSlotTexts;

    private MemberDefinition selectedMember;

    private bool isOpen;

    private void Start()
    {
        menuRoot.SetActive(false);
        Refresh();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        menuRoot.SetActive(isOpen);

        if (isOpen)
            Refresh();
    }

    public void InstallSelectedMember(int slotIndex)
    {
        if (selectedMember == null)
            return;

        if (!memberLoadout.TrySetMember(slotIndex, selectedMember))
            return;

        memberHud.Refresh();
        Refresh();
    }

    private void Refresh()
    {
        RefreshMemberInventory();

        for (int i = 0; i < activeSlotTexts.Length; i++)
        {
            MemberDefinition member = memberLoadout.GetMember(i);

            activeSlotTexts[i].text =
                member != null
                    ? $"{member.Code} // {member.MemberName}"
                    : "[EMPTY]";
        }
    }

    private void RefreshMemberInventory()
    {
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

            TMP_Text text = entry.GetComponentInChildren<TMP_Text>();
            text.text = $"{member.Code} // {member.MemberName}";

            MemberDefinition capturedMember = member;
            entry.onClick.AddListener(() => SelectMember(capturedMember));
        }
    }

    private void SelectMember(MemberDefinition member)
    {
        selectedMember = member;
    }
}