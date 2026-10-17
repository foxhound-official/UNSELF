using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerViewmodelMotion : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Look Sway")]
    [SerializeField] private float lookSwayAmount = 0.02f;
    [SerializeField] private float maximumLookSway = 2.5f;
    [SerializeField] private float lookSwayResponseSpeed = 14f;

    [Header("Movement Inertia")]
    [SerializeField] private float lateralPositionAmount = 0.004f;
    [SerializeField] private float forwardPositionAmount = 0.002f;
    [SerializeField] private float movementRollAmount = 0.7f;
    [SerializeField] private float maximumPositionOffset = 0.025f;
    [SerializeField] private float movementResponseSpeed = 8f;

    [Header("Sprint Pose")]
    [SerializeField] private Vector3 sprintPositionOffset = new Vector3(0f, -0.08f, -0.05f);
    [SerializeField] private Vector3 sprintRotationOffset = new Vector3(5f, 0f, 0f);
    [SerializeField] private float sprintPoseResponseSpeed = 8f;

    [Header("Airborne Pose")]
    [SerializeField] private Vector3 airbornePositionOffset = new Vector3(0f, 0.025f, -0.035f);
    [SerializeField] private Vector3 airborneRotationOffset = new Vector3(-3f, 0f, 0f);
    [SerializeField] private float airbornePoseResponseSpeed = 7f;

    [Header("Slide Pose")]
    [SerializeField] private Vector3 slidePositionOffset = new Vector3(0.04f, -0.14f, -0.08f);
    [SerializeField] private Vector3 slideRotationOffset = new Vector3(8f, 0f, -4f);
    [SerializeField] private float slidePoseResponseSpeed = 10f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Vector2 currentLookSway;
    private Vector3 currentMovementOffset;
    private float currentMovementRoll;

    private Vector3 currentPosePosition;
    private Vector3 currentPoseRotation;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        UpdateLookSway();
        UpdateMovementInertia();
        UpdateLocomotionPose();

        transform.localPosition =
            baseLocalPosition +
            currentMovementOffset +
            currentPosePosition;

        transform.localRotation =
            baseLocalRotation *
            Quaternion.Euler(
                currentLookSway.y + currentPoseRotation.x,
                currentLookSway.x + currentPoseRotation.y,
                currentMovementRoll + currentPoseRotation.z
            );
    }

    private void UpdateLookSway()
    {
        Vector2 mouseDelta = Mouse.current != null
            ? Mouse.current.delta.ReadValue()
            : Vector2.zero;

        Vector2 targetSway = new Vector2(
            -mouseDelta.x * lookSwayAmount,
            mouseDelta.y * lookSwayAmount
        );

        targetSway.x = Mathf.Clamp(targetSway.x, -maximumLookSway, maximumLookSway);
        targetSway.y = Mathf.Clamp(targetSway.y, -maximumLookSway, maximumLookSway);

        float blend = 1f - Mathf.Exp(-lookSwayResponseSpeed * Time.deltaTime);
        currentLookSway = Vector2.Lerp(currentLookSway, targetSway, blend);
    }

    private void UpdateMovementInertia()
    {
        if (playerMovement == null)
            return;

        Vector3 localVelocity =
            playerMovement.transform.InverseTransformDirection(playerMovement.HorizontalVelocity);

        Vector3 targetOffset = new Vector3(
            -localVelocity.x * lateralPositionAmount,
            0f,
            -localVelocity.z * forwardPositionAmount
        );

        targetOffset = Vector3.ClampMagnitude(targetOffset, maximumPositionOffset);

        float targetRoll = -localVelocity.x * movementRollAmount;
        float blend = 1f - Mathf.Exp(-movementResponseSpeed * Time.deltaTime);

        currentMovementOffset = Vector3.Lerp(currentMovementOffset, targetOffset, blend);
        currentMovementRoll = Mathf.Lerp(currentMovementRoll, targetRoll, blend);
    }

    private void UpdateLocomotionPose()
    {
        if (playerMovement == null)
            return;

        Vector3 targetPosition = Vector3.zero;
        Vector3 targetRotation = Vector3.zero;
        float responseSpeed = sprintPoseResponseSpeed;

        if (playerMovement.IsSliding)
        {
            targetPosition = slidePositionOffset;
            targetRotation = slideRotationOffset;
            responseSpeed = slidePoseResponseSpeed;
        }
        else if (!playerMovement.IsGrounded)
        {
            targetPosition = airbornePositionOffset;
            targetRotation = airborneRotationOffset;
            responseSpeed = airbornePoseResponseSpeed;
        }
        else if (playerMovement.IsSprinting)
        {
            targetPosition = sprintPositionOffset;
            targetRotation = sprintRotationOffset;
            responseSpeed = sprintPoseResponseSpeed;
        }

        float blend = 1f - Mathf.Exp(-responseSpeed * Time.deltaTime);

        currentPosePosition = Vector3.Lerp(currentPosePosition, targetPosition, blend);
        currentPoseRotation = Vector3.Lerp(currentPoseRotation, targetRotation, blend);
    }
}