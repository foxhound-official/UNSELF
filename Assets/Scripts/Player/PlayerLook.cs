using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.08f;

    [Header("Hang Look")]
    [SerializeField] private float hangHorizontalLookLimit = 70f;

    private float verticalRotation;
    private float hangHorizontalRotation;
    private bool isHangLook;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        if (isHangLook)
        {
            hangHorizontalRotation += mouseX;
            hangHorizontalRotation = Mathf.Clamp(
                hangHorizontalRotation,
                -hangHorizontalLookLimit,
                hangHorizontalLookLimit
            );
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        ApplyCameraRotation();
    }

    public void EnterHangLook()
    {
        isHangLook = true;
        hangHorizontalRotation = 0f;

        ApplyCameraRotation();
    }

    public void ExitHangLookPreservingView()
    {
        if (!isHangLook)
            return;

        transform.Rotate(Vector3.up * hangHorizontalRotation);

        hangHorizontalRotation = 0f;
        isHangLook = false;

        ApplyCameraRotation();
    }

    public void AddRecoil(float pitch, float yaw)
    {
        verticalRotation -= pitch;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        if (isHangLook)
        {
            hangHorizontalRotation += yaw;
            hangHorizontalRotation = Mathf.Clamp(
                hangHorizontalRotation,
                -hangHorizontalLookLimit,
                hangHorizontalLookLimit
            );
        }
        else
        {
            transform.Rotate(Vector3.up * yaw);
        }

        ApplyCameraRotation();
    }

    public void ResetRotation()
    {
        verticalRotation = 0f;
        hangHorizontalRotation = 0f;

        ApplyCameraRotation();
    }

    private void ApplyCameraRotation()
    {
        float horizontalRotation = isHangLook
            ? hangHorizontalRotation
            : 0f;

        cameraTransform.localRotation = Quaternion.Euler(
            verticalRotation,
            horizontalRotation,
            0f
        );
    }
}