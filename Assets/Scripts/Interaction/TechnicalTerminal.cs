using UnityEngine;
using TMPro;

public class TechnicalTerminal : MonoBehaviour, IInteractable, IMemberPerceptionTarget
{
    public string InteractionPrompt => "ACCESS TERMINAL";

    [SerializeField] private MemberLoadout memberLoadout;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private SimpleDoor controlledDoor;
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private float logCooldown = 3f;
    private float nextLogTime;
    private Color[] normalColors;

    private void Awake()
    {
        normalColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            normalColors[i] = targetRenderers[i].material.color;
        }
    }

    public void OnMemberFocusEnter(MemberLoadout loadout)
    {
        MemberDefinition technicalMember = loadout.GetFirstMemberByClass(MemberClass.Technical);

        if (technicalMember == null)
            return;

        SetHighlighted(true);

        if (Time.time < nextLogTime)
            return;

        memberHud.ShowMemberLog(technicalMember, "TECHNICAL ACCESS AVAILABLE");
        nextLogTime = Time.time + logCooldown;
    }

    public void OnMemberFocusExit()
    {
        SetHighlighted(false);
    }

    public void Interact()
    {
        MemberDefinition technicalMember =
            memberLoadout.GetFirstMemberByClass(MemberClass.Technical);

        if (technicalMember == null)
        {
            statusText.text = "ACCESS DENIED";
            return;
        }

        if (controlledDoor.IsOpen)
        {
            statusText.text = "ACCESS OPEN";
            memberHud.ShowMemberLog(technicalMember, "ACCESS ALREADY OPEN");
            return;
        }

        statusText.text = "ACCESS GRANTED";

        controlledDoor.Unlock();
        controlledDoor.Open();

        memberHud.ShowMemberLog(technicalMember, "ACCESS AUTHORIZED");
        memberHud.ShowMemberLog(technicalMember, "DOOR CONTROL OVERRIDDEN");
    }

    private void SetHighlighted(bool highlighted)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer targetRenderer = targetRenderers[i];

            if (targetRenderer == null)
                continue;

            targetRenderer.material.color = highlighted
                ? normalColors[i] * 1.35f
                : normalColors[i];
        }
    }
}