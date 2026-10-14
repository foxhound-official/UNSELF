using UnityEngine;

public class InteractionController : MonoBehaviour, IInteractable, IConditionalInteractable
{
    [SerializeField] private string interactionPrompt = "INTERACT";
    [SerializeField] private InteractionRequirement[] requirements;
    [SerializeField] private WorldAction[] actions;

    public string InteractionPrompt => interactionPrompt;
    public bool CanInteract => AreRequirementsMet();

    public void Interact()
    {
        if (!CanInteract)
            return;

        foreach (WorldAction action in actions)
        {
            if (action != null)
                action.Execute();
        }
    }

    private bool AreRequirementsMet()
    {
        if (requirements == null)
            return true;

        foreach (InteractionRequirement requirement in requirements)
        {
            if (requirement != null && !requirement.IsMet)
                return false;
        }

        return true;
    }
}