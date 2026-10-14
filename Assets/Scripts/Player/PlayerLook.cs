using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.08f;

    private float verticalRotation;

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

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        ApplyCameraRotation();
    }

    public void AddRecoil(float pitch, float yaw)
    {
        verticalRotation -= pitch;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        transform.Rotate(Vector3.up * yaw);

        ApplyCameraRotation();
    }

    public void ResetRotation()
    {
        verticalRotation = 0f;
        ApplyCameraRotation();
    }

    private void ApplyCameraRotation()
    {
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}