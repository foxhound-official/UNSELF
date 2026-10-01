using UnityEngine;

public class TechnicalTerminal : MonoBehaviour, IInteractable, IMemberPerceptionTarget
{
    public string InteractionPrompt => "ACCESS TERMINAL";

    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private float logCooldown = 3f;
    private float nextLogTime;
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

        Color highlightColor = normalColor * 1.35f;
        targetRenderer.material.color = highlightColor;

        if (Time.time < nextLogTime)
            return;

        memberHud.ShowMemberLog(technicalMember, "TECHNICAL ACCESS AVAILABLE");
        nextLogTime = Time.time + logCooldown;
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