using UnityEngine;

public class DialogueNPC :
    MonoBehaviour,
    IInteractable,
    IMemberPerceptionTarget
{
    [SerializeField] private DialogueController dialogueController;

    [Header("NPC")]
    [SerializeField] private string speakerName = "WORKER";

    [TextArea]
    [SerializeField] private string openingLine;

    [Header("I Choice")]
    [TextArea]
    [SerializeField] private string defaultChoice;

    [TextArea]
    [SerializeField] private string defaultResponse;

    [Header("Member Choice")]
    [SerializeField] private MemberDefinition requiredMember;

    [TextArea]
    [SerializeField] private string memberChoice;

    [TextArea]
    [SerializeField] private string memberResponse;

    [Header("Result")]
    [SerializeField] private SimpleDoor controlledDoor;

    [Header("Perception")]
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private float logCooldown = 3f;

    private Color[] normalColors;
    private float nextLogTime;
    private bool dialogueCompleted;

    public string InteractionPrompt => "TALK";

    public string SpeakerName => speakerName;
    public string OpeningLine => openingLine;

    public string DefaultChoice => defaultChoice;
    public string DefaultResponse => defaultResponse;

    public MemberDefinition RequiredMember => requiredMember;
    public string MemberChoice => memberChoice;
    public string MemberResponse => memberResponse;

    private void Awake()
    {
        normalColors =
            new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            normalColors[i] =
                targetRenderers[i].material.color;
        }
    }

    public void Interact()
    {
        dialogueController.OpenDialogue(this);
    }

    public void CompleteMemberDialogue()
    {
        if (dialogueCompleted)
            return;

        dialogueCompleted = true;

        if (controlledDoor == null)
            return;

        controlledDoor.Unlock();
        controlledDoor.Open();
    }

    public void OnMemberFocusEnter(
        MemberAssembly memberAssembly)
    {
        if (requiredMember == null ||
            !memberAssembly.HasMember(requiredMember))
        {
            return;
        }

        SetHighlighted(true);

        if (Time.time < nextLogTime)
            return;

        if (memberHud != null)
        {
            memberHud.ShowMemberLog(
                requiredMember,
                "SOCIAL APPROACH AVAILABLE"
            );
        }

        nextLogTime =
            Time.time + logCooldown;
    }

    public void OnMemberFocusExit()
    {
        SetHighlighted(false);
    }

    private void SetHighlighted(bool highlighted)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer targetRenderer =
                targetRenderers[i];

            if (targetRenderer == null)
                continue;

            targetRenderer.material.color =
                highlighted
                    ? normalColors[i] * 1.35f
                    : normalColors[i];
        }
    }
}