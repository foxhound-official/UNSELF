using UnityEngine;

public class CapabilityPerceptionFeedback : MonoBehaviour, IMemberPerceptionTarget
{
    [SerializeField] private MemberCapability requiredCapability;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Feedback")]
    [SerializeField] private string logMessage = "CAPABILITY AVAILABLE";
    [SerializeField] private float highlightMultiplier = 1.35f;
    [SerializeField] private float logCooldown = 3f;

    private Color[] normalColors;
    private float nextLogTime;

    private void Awake()
    {
        normalColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                normalColors[i] = targetRenderers[i].material.color;
        }
    }

    public void OnMemberFocusEnter(MemberAssembly memberAssembly)
    {
        MemberDefinition member = memberAssembly.GetFirstMemberWithCapability(requiredCapability);

        if (member == null)
            return;

        SetHighlighted(true);

        if (Time.time < nextLogTime)
            return;

        if (memberHud != null)
            memberHud.ShowMemberLog(member, logMessage);

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

            targetRenderer.material.color = highlighted ? normalColors[i] * highlightMultiplier : normalColors[i];
        }
    }
}