using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private InteractionPromptUI interactionPrompt;

    private void Update()
    {
        UpdateInteractionPrompt();

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            TryInteract();
    }

    private void UpdateInteractionPrompt()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            interactionPrompt.Hide();
            return;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            interactionPrompt.Hide();
            return;
        }

        interactionPrompt.Show($"[F] {interactable.InteractionPrompt}");
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
            return;

        interactable.Interact();
    }
}