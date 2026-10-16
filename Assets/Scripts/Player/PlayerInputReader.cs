using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool CrouchHeld { get; private set; }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            ResetInput();
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        Move = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        JumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        SprintHeld = Keyboard.current.leftShiftKey.isPressed;
        CrouchPressed = Keyboard.current.leftCtrlKey.wasPressedThisFrame;
        CrouchHeld = Keyboard.current.leftCtrlKey.isPressed;
    }

    private void ResetInput()
    {
        Move = Vector2.zero;
        JumpPressed = false;
        SprintHeld = false;
        CrouchPressed = false;
        CrouchHeld = false;
    }
}