using UnityEngine;

public class BreakableBarricade : MonoBehaviour, IInteractable, IConditionalInteractable, IMemberPerceptionTarget
{
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private MemberDefinition requiredMember;
    [SerializeField] private Renderer targetRenderer;

    [Header("Perception")]
    [SerializeField] private float logCooldown = 3f;

    private Color normalColor;
    private float nextLogTime;
    private bool isBroken;

    public string InteractionPrompt => "BREAK BARRICADE";

    public bool CanInteract => !isBroken && memberAssembly.HasMember(requiredMember);

    private void Awake()
    {
        if (targetRenderer != null)
            normalColor = targetRenderer.material.color;
    }

    public void OnMemberFocusEnter(MemberAssembly assembly)
    {
        if (!assembly.HasMember(requiredMember) || isBroken)
            return;

        SetHighlighted(true);

        if (Time.time < nextLogTime)
            return;

        memberHud.ShowMemberLog(
            requiredMember,
            "BREACH POINT DETECTED"
        );

        nextLogTime = Time.time + logCooldown;
    }

    public void OnMemberFocusExit()
    {
        SetHighlighted(false);
    }

    public void Interact()
    {
        if (!CanInteract)
            return;

        isBroken = true;

        memberHud.ShowMemberLog(requiredMember, "BARRIER BREACHED");

        gameObject.SetActive(false);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (targetRenderer == null)
            return;

        targetRenderer.material.color = highlighted
            ? normalColor * 1.35f
            : normalColor;
    }
}