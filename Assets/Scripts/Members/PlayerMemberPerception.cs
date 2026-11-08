using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMemberPerception : MonoBehaviour
{
    [Header("Perception")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private float perceptionDistance = 8f;

    [Header("Perception Visibility")]
    [SerializeField] private LayerMask perceptionTransparentLayers;

    private IMemberPerceptionTarget currentTarget;
    private WorldMarkerManager markerManager;

    private WorldMarkerTarget suppressedTarget;
    private InputAction clearMarkersAction;

    private void Awake()
    {
        clearMarkersAction =
            GameControls.GetPlayerAction("ClearMarkers");
    }

    private void Start()
    {
        markerManager =
            FindAnyObjectByType<WorldMarkerManager>();
    }

    private void Update()
    {
        if (playerCamera == null)
            return;

        IMemberPerceptionTarget newTarget = null;
        WorldMarkerTarget markerTarget = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        // Стекло не блокирует обнаружение,
        // остальные препятствия продолжают работать.
        int perceptionMask =
            Physics.DefaultRaycastLayers &
            ~perceptionTransparentLayers.value;

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            perceptionDistance,
            perceptionMask,
            QueryTriggerInteraction.UseGlobal))
        {
            newTarget =
                hit.collider.GetComponentInParent<
                    IMemberPerceptionTarget>();

            markerTarget =
                hit.collider.GetComponentInParent<
                    WorldMarkerTarget>();
        }

        // Сохраняем существующую логику восприятия.
        if (!ReferenceEquals(newTarget, currentTarget))
        {
            currentTarget?.OnMemberFocusExit();

            currentTarget = newTarget;
            currentTarget?.OnMemberFocusEnter(memberAssembly);
        }

        // Если игрок отвёл взгляд от объекта,
        // разрешаем его повторное обнаружение.
        if (markerTarget != suppressedTarget)
            suppressedTarget = null;

        // Ручная очистка экрана.
        if (clearMarkersAction.WasPressedThisFrame() &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            markerManager?.ClearAll();

            // Не позволяем текущей цели немедленно
            // восстановить свою метку.
            suppressedTarget = markerTarget;
        }

        // Обнаружение мировых меток.
        if (markerManager != null &&
            markerTarget != null &&
            markerTarget != suppressedTarget &&
            markerTarget.CanReveal(memberAssembly))
        {
            MemberDefinition sourceMember =
                markerTarget.ResolveSourceMember(memberAssembly);

            markerManager.Reveal(
                markerTarget,
                sourceMember
            );
        }
    }

    private void OnDisable()
    {
        currentTarget?.OnMemberFocusExit();
        currentTarget = null;
        suppressedTarget = null;
    }
}