using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssemblySlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text slotNumberText;
    [SerializeField] private TMP_Text memberNameText;

    [SerializeField] private Button slotButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button hudButton;

    [SerializeField] private TMP_Text hudButtonText;

    private int slotIndex;
    private MemberMenuController menuController;

    public void Initialize(int index, MemberMenuController controller)
    {
        slotIndex = index;
        menuController = controller;

        slotNumberText.text = $"{slotIndex + 1:00}";

        slotButton.onClick.AddListener(() =>
            menuController.InstallSelectedMember(slotIndex));

        removeButton.onClick.AddListener(() =>
            menuController.RemoveMember(slotIndex));

        hudButton.onClick.AddListener(() =>
            menuController.ToggleHudMember(slotIndex));
    }

    public void Refresh(MemberAssembly assembly, MemberLoadout loadout)
    {
        slotNumberText.text = $"{slotIndex + 1:00}";

        if (!assembly.IsSlotUnlocked(slotIndex))
        {
            memberNameText.text = "[LOCKED]";

            slotButton.interactable = false;
            removeButton.gameObject.SetActive(false);
            hudButton.gameObject.SetActive(false);
            return;
        }

        slotButton.interactable = true;

        MemberDefinition member = assembly.GetMember(slotIndex);

        if (member == null)
        {
            memberNameText.text = "[EMPTY]";

            removeButton.gameObject.SetActive(false);
            hudButton.gameObject.SetActive(false);
            return;
        }

        bool isStartSlot = assembly.IsMemberStartSlot(slotIndex);

        if (!isStartSlot)
        {
            // Continuation slots only indicate space occupied by a multi-slot Member.
            memberNameText.text = $"[{member.Code}]";

            removeButton.gameObject.SetActive(false);
            hudButton.gameObject.SetActive(false);
            return;
        }

        memberNameText.text = $"{member.Code} // {member.MemberName}";

        removeButton.gameObject.SetActive(true);
        hudButton.gameObject.SetActive(true);

        hudButtonText.text = loadout.IsMemberVisible(member)
            ? "[HUD]"
            : "[   ]";
    }
}