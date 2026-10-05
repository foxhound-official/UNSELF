using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemberArchiveEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text memberNameText;
    [SerializeField] private TMP_Text slotCostText;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image accentImage;

    public void Refresh(
        MemberDefinition member,
        bool isSelected)
    {
        Color32 memberColor =
            GetMemberColor(member.Class);

        codeText.text = member.Code;
        memberNameText.text = member.MemberName;
        slotCostText.text = member.SlotCost == 1 ? "1 SLOT" : $"{member.SlotCost} SLOTS";

        codeText.color = memberColor;

        memberNameText.color =
            isSelected
                ? memberColor
                : new Color32(
                    255,
                    255,
                    255,
                    185
                );

        accentImage.color =
            isSelected
                ? memberColor
                : WithAlpha(
                    memberColor,
                    100
                );

        backgroundImage.color =
            isSelected
                ? WithAlpha(
                    memberColor,
                    18
                )
                : new Color32(
                    255,
                    255,
                    255,
                    5
                );
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