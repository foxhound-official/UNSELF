
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    public Vector2 Move { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }

    public bool SprintHeld { get; private set; }

    public bool CrouchPressed { get; private set; }
    public bool CrouchHeld { get; private set; }

    private void Awake()
    {
        moveAction = GameControls.GetPlayerAction("Move");
        jumpAction = GameControls.GetPlayerAction("Jump");
        sprintAction = GameControls.GetPlayerAction("Sprint");
        crouchAction = GameControls.GetPlayerAction("Crouch");
    }

    private void Update()
    {
        Move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        JumpPressed = jumpAction.WasPressedThisFrame();
        JumpHeld = jumpAction.IsPressed();

        SprintHeld = sprintAction.IsPressed();

        CrouchPressed = crouchAction.WasPressedThisFrame();
        CrouchHeld = crouchAction.IsPressed();
    }

    private void OnDisable()
    {
        Move = Vector2.zero;
        JumpPressed = false;
        JumpHeld = false;
        SprintHeld = false;
        CrouchPressed = false;
        CrouchHeld = false;
    }
}