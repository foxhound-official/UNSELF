
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DoorInteractable : MonoBehaviour, IInteractable, IConditionalInteractable
{
    [Header("Door Rotation")]
    [SerializeField, Range(-180f, 180f)] private float openAngle = 95f;
    [SerializeField, Min(1f)] private float rotationSpeed = 140f;

    [Header("Initial State")]
    [SerializeField] private bool startOpened;

    private Rigidbody doorRigidbody;

    private Quaternion closedLocalRotation;
    private Quaternion openedLocalRotation;

    private bool isOpen;
    private bool isMoving;

    public bool IsOpen => isOpen;
    public bool CanInteract => !isMoving;

    public string InteractionPrompt =>
        isOpen ? "ЗАКРЫТЬ ДВЕРЬ" : "ОТКРЫТЬ ДВЕРЬ";

    private void Awake()
    {
        doorRigidbody = GetComponent<Rigidbody>();

        closedLocalRotation = transform.localRotation;
        openedLocalRotation = closedLocalRotation *
            Quaternion.Euler(0f, openAngle, 0f);

        isOpen = startOpened;

        if (startOpened)
        {
            transform.localRotation = openedLocalRotation;
            doorRigidbody.rotation = transform.rotation;
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving)
            return;

        Quaternion targetLocalRotation =
            isOpen ? openedLocalRotation : closedLocalRotation;

        Quaternion parentRotation = transform.parent != null
            ? transform.parent.rotation
            : Quaternion.identity;

        Quaternion targetWorldRotation =
            parentRotation * targetLocalRotation;

        Quaternion nextRotation = Quaternion.RotateTowards(
            doorRigidbody.rotation,
            targetWorldRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        doorRigidbody.MoveRotation(nextRotation);

        if (Quaternion.Angle(nextRotation, targetWorldRotation) > 0.1f)
            return;

        isMoving = false;
    }

    public void Interact()
    {
        if (!CanInteract)
            return;

        isOpen = !isOpen;
        isMoving = true;
    }
}
