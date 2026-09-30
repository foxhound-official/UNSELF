using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField] private float crouchingCameraHeight = 0.4f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        standingHeight = characterController.height;
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y += 1;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        float currentSpeed = moveSpeed;

        bool isCrouching =
            Keyboard.current != null &&
            Keyboard.current.leftCtrlKey.isPressed;

        bool isSprinting =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            input.y > 0f &&
            !isCrouching;

        if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }

        Vector3 movement =
            transform.forward * input.y +
            transform.right * input.x;

        if (characterController.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        if (characterController.isGrounded &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        movement.y = verticalVelocity;

        characterController.Move(movement * currentSpeed * Time.deltaTime);

        if (isCrouching)
        {
            characterController.height = crouchingHeight;
            characterController.center = new Vector3(0f, crouchingHeight / 2f, 0f);

            cameraTransform.localPosition = new Vector3(
                0f,
                crouchingCameraHeight,
                0f
            );
        }
        else
        {
            characterController.height = standingHeight;
            characterController.center = new Vector3(
                0f,
                standingHeight / 2f,
                0f
            );

            cameraTransform.localPosition = new Vector3(
                0f,
                0.65f,
                0f
            );
        }
    }
}