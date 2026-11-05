
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DoorInteractable : MonoBehaviour, IInteractable, IConditionalInteractable
{
    [Header("Door Rotation")]
    [SerializeField, Range(-180f, 180f)]
    private float openAngle = 95f;

    [SerializeField, Min(1f)]
    private float rotationSpeed = 140f;

    [Header("Initial State")]
    [SerializeField] private bool startOpened;
    [SerializeField] private bool startLocked;

    private Rigidbody doorRigidbody;

    private Quaternion closedLocalRotation;
    private Quaternion openedLocalRotation;

    private bool isOpen;
    private bool isMoving;
    private bool isLocked;

    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;
    public bool CanInteract => !isMoving;

    public string InteractionPrompt =>
        isLocked
            ? "ДВЕРЬ ЗАПЕРТА"
            : isOpen
                ? "ЗАКРЫТЬ ДВЕРЬ"
                : "ОТКРЫТЬ ДВЕРЬ";

    private void Awake()
    {
        doorRigidbody = GetComponent<Rigidbody>();

        closedLocalRotation = transform.localRotation;
        openedLocalRotation = closedLocalRotation *
            Quaternion.Euler(0f, openAngle, 0f);

        isLocked = startLocked;
        isOpen = startOpened && !isLocked;

        if (isOpen)
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
        if (!CanInteract || isLocked)
            return;

        isOpen = !isOpen;
        isMoving = true;
    }

    public void Lock()
    {
        if (isOpen || isMoving)
            return;

        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }
}
