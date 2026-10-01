using UnityEngine;

public class TechnicalTerminal : MonoBehaviour, IInteractable, IMemberPerceptionTarget
{
    public string InteractionPrompt => "ACCESS TERMINAL";

    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private SimpleDoor controlledDoor;

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
        MemberDefinition technicalMember =
            memberLoadout.GetFirstMemberByClass(MemberClass.Technical);

        if (technicalMember == null)
        {
            Debug.Log("TERMINAL: ACCESS DENIED");
            return;
        }

        if (controlledDoor.IsOpen)
        {
            memberHud.ShowMemberLog(technicalMember, "ACCESS ALREADY OPEN");
            return;
        }

        controlledDoor.Unlock();
        controlledDoor.Open();

        memberHud.ShowMemberLog(technicalMember, "ACCESS AUTHORIZED");
        memberHud.ShowMemberLog(technicalMember, "DOOR CONTROL OVERRIDDEN");
    }
}