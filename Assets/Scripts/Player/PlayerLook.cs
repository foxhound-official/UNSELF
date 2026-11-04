using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.08f;

    [Header("Ledge Climb Look")]
    [SerializeField, Range(-15f, 15f)] private float ledgeClimbTargetPitch = 0f;

    [SerializeField, Min(1f)] private float ledgeClimbPitchSpeed = 300f;

    private TraversalMotor traversalMotor;

    public float Pitch => verticalRotation;

    private float horizontalRotation;
    private float verticalRotation;

    public Quaternion DesiredBodyRotation =>
        Quaternion.Euler(0f, horizontalRotation, 0f);

    private void Awake()
    {
        horizontalRotation = transform.eulerAngles.y;
        traversalMotor = GetComponent<TraversalMotor>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (traversalMotor != null && traversalMotor.IsLedgeClimbing)
        {
            verticalRotation = Mathf.MoveTowardsAngle(
                verticalRotation,
                ledgeClimbTargetPitch,
                ledgeClimbPitchSpeed * Time.deltaTime
            );

            ApplyCameraRotation();
            return;
        }
        
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float sensitivity = mouseSensitivity * GameControls.MouseSensitivityMultiplier;

        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        horizontalRotation += mouseX;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        ApplyCameraRotation();
    }

    public void AddRecoil(float pitch, float yaw)
    {
        verticalRotation -= pitch;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        horizontalRotation += yaw;

        ApplyCameraRotation();
    }

    public void ResetRotation()
    {
        verticalRotation = 0f;
        ApplyCameraRotation();
    }

    private void ApplyCameraRotation()
    {
        cameraTransform.localRotation =
            Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}