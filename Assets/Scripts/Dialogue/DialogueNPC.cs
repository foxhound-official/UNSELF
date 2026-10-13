using UnityEngine;

public class DialogueNPC : MonoBehaviour, IInteractable, IMemberPerceptionTarget
{
    [SerializeField] private DialogueController dialogueController;

    [Header("Dialogue")]
    [SerializeField] private string startNode = "TestWorker";

    [Header("Member")]
    [SerializeField] private MemberDefinition requiredMember;

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
    public string StartNode => startNode;
    public MemberDefinition RequiredMember => requiredMember;

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

    public void OnMemberFocusEnter(MemberAssembly memberAssembly)
    {
        if (requiredMember == null || !memberAssembly.HasMember(requiredMember))
            return;

        SetHighlighted(true);

        if (Time.time < nextLogTime)
            return;

        if (memberHud != null)
            memberHud.ShowMemberLog(requiredMember, "SOCIAL APPROACH AVAILABLE");

        nextLogTime = Time.time + logCooldown;
    }

    public void OnMemberFocusExit()
    {
        SetHighlighted(false);
    }

    private void SetHighlighted(bool highlighted)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer targetRenderer = targetRenderers[i];

            if (targetRenderer == null)
                continue;

            targetRenderer.material.color = highlighted ? normalColors[i] * 1.35f : normalColors[i];
        }
    }
}