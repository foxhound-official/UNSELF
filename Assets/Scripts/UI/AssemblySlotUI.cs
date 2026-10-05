using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssemblySlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text slotNumberText;
    [SerializeField] private TMP_Text memberNameText;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image accentImage;

    [SerializeField] private Button slotButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button hudButton;

    [SerializeField] private TMP_Text hudButtonText;

    private static readonly Color32 SecondaryColor =
        new(255, 255, 255, 105);

    private static readonly Color32 EmptyColor =
        new(255, 255, 255, 70);

    private static readonly Color32 LockedColor =
        new(255, 255, 255, 35);

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

    public void Refresh(
        MemberAssembly assembly,
        MemberLoadout loadout,
        bool canEditAssembly)
    {
        slotNumberText.text = $"{slotIndex + 1:00}";
        slotNumberText.color = SecondaryColor;

        if (!assembly.IsSlotUnlocked(slotIndex))
        {
            memberNameText.text = "[LOCKED]";
            memberNameText.color = LockedColor;

            backgroundImage.color =
                new Color32(255, 255, 255, 3);

            accentImage.gameObject.SetActive(false);

            slotButton.interactable = false;
            removeButton.gameObject.SetActive(false);
            hudButton.gameObject.SetActive(false);
            return;
        }

        MemberDefinition member =
            assembly.GetMember(slotIndex);

        if (member == null)
        {
            memberNameText.text = "[EMPTY]";
            memberNameText.color = EmptyColor;

            backgroundImage.color =
                new Color32(255, 255, 255, 6);

            accentImage.gameObject.SetActive(false);

            slotButton.interactable = canEditAssembly;
            removeButton.gameObject.SetActive(false);
            hudButton.gameObject.SetActive(false);
            return;
        }

        Color32 memberColor =
            GetMemberColor(member.Class);

        slotButton.interactable = false;

        bool isStartSlot =
            assembly.IsMemberStartSlot(slotIndex);

        if (!isStartSlot)
        {
            // Continuation slots visually belong to the same multi-slot Member.
            memberNameText.text = $"[{member.Code}]";
            memberNameText.color =
                WithAlpha(memberColor, 150);

            backgroundImage.color =
                WithAlpha(memberColor, 8);

            accentImage.gameObject.SetActive(true);
            accentImage.color =
                WithAlpha(memberColor, 120);

            removeButton.gameObject.SetActive(false);
            hudButton.gameObject.SetActive(false);
            return;
        }

        memberNameText.text =
            $"{member.Code} // {member.MemberName}";

        memberNameText.color = memberColor;

        backgroundImage.color =
            WithAlpha(memberColor, 14);

        accentImage.gameObject.SetActive(true);
        accentImage.color = memberColor;

        removeButton.gameObject.SetActive(canEditAssembly);
        hudButton.gameObject.SetActive(true);

        bool isVisible =
            loadout.IsMemberVisible(member);

        hudButtonText.text =
            isVisible
                ? "[HUD]"
                : "[   ]";

        hudButtonText.color =
            isVisible
                ? memberColor
                : SecondaryColor;
    }

    private static Color32 GetMemberColor(
        MemberClass memberClass)
    {
        return memberClass switch
        {
            MemberClass.Social =>
                new Color32(77, 179, 255, 255),

            MemberClass.Technical =>
                new Color32(255, 191, 51, 255),

            MemberClass.Combat =>
                new Color32(255, 77, 64, 255),

            _ =>
                new Color32(255, 255, 255, 255)
        };
    }

    private static Color32 WithAlpha(
        Color32 color,
        byte alpha)
    {
        return new Color32(
            color.r,
            color.g,
            color.b,
            alpha
        );
    }
}