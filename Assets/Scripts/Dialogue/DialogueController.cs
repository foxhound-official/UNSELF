using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

public class DialogueController : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private MemberAssembly memberAssembly;

    private DialogueNPC currentNpc;

    private bool movementWasEnabled;
    private bool lookWasEnabled;
    private bool interactionWasEnabled;
    private bool playerLocked;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        // Yarn owns dialogue flow, while UNSELF owns player and gameplay state.
        dialogueRunner.onDialogueComplete.AddListener(HandleDialogueCompleted);

        // Temporary bridges until the universal capability and action systems are ready.
        dialogueRunner.AddFunction("has_required_member", (Func<bool>)HasRequiredMember);
        dialogueRunner.AddCommandHandler("complete_member_dialogue", (Action)CompleteMemberDialogue);
    }

    private void Update()
    {
        if (!dialogueRunner.IsDialogueRunning || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            _ = dialogueRunner.Stop();
    }

    private void OnDestroy()
    {
        if (dialogueRunner == null)
            return;

        dialogueRunner.onDialogueComplete.RemoveListener(HandleDialogueCompleted);
        dialogueRunner.RemoveFunction("has_required_member");
        dialogueRunner.RemoveCommandHandler("complete_member_dialogue");
    }

    public void OpenDialogue(DialogueNPC npc)
    {
        if (npc == null || dialogueRunner.IsDialogueRunning)
            return;

        currentNpc = npc;

        LockPlayer();
        _ = dialogueRunner.StartDialogue(npc.StartNode);
    }

    private void LockPlayer()
    {
        if (playerLocked)
            return;

        movementWasEnabled = playerMovement.enabled;
        lookWasEnabled = playerLook.enabled;
        interactionWasEnabled = playerInteraction.enabled;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        playerMovement.enabled = false;
        playerLook.enabled = false;
        playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerLocked = true;
    }

    private void HandleDialogueCompleted()
    {
        UnlockPlayer();
        currentNpc = null;
    }

    private void UnlockPlayer()
    {
        if (!playerLocked)
            return;

        playerMovement.enabled = movementWasEnabled;
        playerLook.enabled = lookWasEnabled;
        playerInteraction.enabled = interactionWasEnabled;

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        playerLocked = false;
    }

    private bool HasRequiredMember()
    {
        return currentNpc != null &&
               currentNpc.RequiredMember != null &&
               memberAssembly.HasMember(currentNpc.RequiredMember);
    }

    private void CompleteMemberDialogue()
    {
        if (currentNpc == null)
            return;

        currentNpc.CompleteMemberDialogue();
    }
}