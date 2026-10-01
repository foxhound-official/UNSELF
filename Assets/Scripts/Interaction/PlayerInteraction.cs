using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 2.5f;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
            return;

        TryInteract();
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