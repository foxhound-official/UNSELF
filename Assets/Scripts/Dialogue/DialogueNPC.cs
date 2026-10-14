using System;
using UnityEngine;

public class DialogueNPC : MonoBehaviour, IInteractable
{
    [Serializable]
    private class DialogueAction
    {
        public string id;
        public WorldAction[] actions;
    }

    [SerializeField] private DialogueController dialogueController;

    [Header("Dialogue")]
    [SerializeField] private string startNode = "TestWorker";
    [SerializeField] private DialogueAction[] dialogueActions;

    public string InteractionPrompt => "TALK";
    public string StartNode => startNode;

    public void Interact()
    {
        dialogueController.OpenDialogue(this);
    }

    public void ExecuteAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return;

        foreach (DialogueAction dialogueAction in dialogueActions)
        {
            if (dialogueAction.id != actionId)
                continue;

            foreach (WorldAction action in dialogueAction.actions)
            {
                if (action != null)
                    action.Execute();
            }

            return;
        }
    }
}