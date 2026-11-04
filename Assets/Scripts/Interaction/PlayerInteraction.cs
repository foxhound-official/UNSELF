using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private InteractionPromptUI interactionPrompt;
    private InputAction interactAction;
    private string interactKeyLabel;

    private void Awake()
    {
        interactAction = GameControls.GetPlayerAction("Interact");

        UpdateInteractKeyLabel();
        GameControls.BindingsChanged += UpdateInteractKeyLabel;
    }

    private void OnDestroy()
    {
        GameControls.BindingsChanged -= UpdateInteractKeyLabel;
    }

    private void UpdateInteractKeyLabel()
    {
        interactKeyLabel = GameControls.GetBindingLabel("Interact");
    }

    private void Update()
    {
        UpdateInteractionPrompt();

        if (interactAction.WasPressedThisFrame())
            TryInteract();
    }

    private void OnDisable()
    {
        HideInteractionPrompt();
    }

    private void UpdateInteractionPrompt()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            HideInteractionPrompt();
            return;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            HideInteractionPrompt();
            return;
        }

        if (interactable is IConditionalInteractable conditional && !conditional.CanInteract)
        {
            HideInteractionPrompt();
            return;
        }

        if (interactionPrompt != null)
            interactionPrompt.Show($"[{interactKeyLabel}] {interactable.InteractionPrompt}");
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
            return;

        if (interactable is IConditionalInteractable conditional && !conditional.CanInteract)
            return;

        interactable.Interact();
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.Hide();
    }
}