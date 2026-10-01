using UnityEngine;

public class TechnicalTerminal : MonoBehaviour, IInteractable, IMemberPerceptionTarget
{
    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private Renderer targetRenderer;

    private Color normalColor;

    private void Awake()
    {
        normalColor = targetRenderer.material.color;
    }

    public void OnMemberFocusEnter(MemberLoadout loadout)
    {
        MemberDefinition technicalMember = loadout.GetFirstMemberByClass(MemberClass.Technical);

        if (technicalMember == null)
            return;

        memberHud.ShowMemberLog(technicalMember, "TECHNICAL ACCESS AVAILABLE");

        Color highlightColor = normalColor * 1.35f;
        targetRenderer.material.color = highlightColor;
    }

    public void OnMemberFocusExit()
    {
        targetRenderer.material.color = normalColor;
    }

    public void Interact()
    {
        if (memberLoadout.HasMemberClass(MemberClass.Technical))
        {
            Debug.Log("TERMINAL: TECHNICAL ACCESS GRANTED");
            return;
        }

        Debug.Log("TERMINAL: ACCESS DENIED");
    }
}