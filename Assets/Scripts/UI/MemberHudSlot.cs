using TMPro;
using UnityEngine;

public class MemberHudSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color emptyColor = new Color(0.55f, 0.55f, 0.55f);

    private void Awake()
    {
        SetEmpty();
    }

    public void SetEmpty()
    {
        headerText.text = "[EMPTY]";
        statusText.text = "> NO ACTIVE MEMBER";

        headerText.color = emptyColor;
        statusText.color = emptyColor;
    }

    public void SetMember(MemberDefinition member)
    {
        Color color = GetClassColor(member.Class);

        headerText.text = $"{member.Code} // {member.MemberName}";
        statusText.text = $"> {member.Class.ToString().ToUpper()} MEMBER\n> ACTIVE";

        headerText.color = color;
        statusText.color = color;
    }

    private Color GetClassColor(MemberClass memberClass)
    {
        return memberClass switch
        {
            MemberClass.Social => new Color(0.30f, 0.70f, 1.00f),
            MemberClass.Technical => new Color(1.00f, 0.75f, 0.20f),
            MemberClass.Combat => new Color(1.00f, 0.30f, 0.25f),
            _ => Color.white
        };
    }
}