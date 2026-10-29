using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugSectorTeleports : MonoBehaviour
{
    private static readonly Key[] Hotkeys =
    {
        Key.F1, Key.F2, Key.F3, Key.F4,
        Key.F5, Key.F6, Key.F7, Key.F8,
        Key.F9, Key.F10, Key.F11, Key.F12
    };

    [SerializeField] private KinematicCharacterMotor playerMotor;

    private PlayerMovement playerMovement;
    private TraversalMotor traversalMotor;

    private void Awake()
    {
        if (playerMotor == null)
            return;

        playerMovement = playerMotor.GetComponent<PlayerMovement>();
        traversalMotor = playerMotor.GetComponent<TraversalMotor>();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (playerMotor == null || Keyboard.current == null)
            return;

        for (int i = 0; i < Hotkeys.Length; i++)
        {
            if (!Keyboard.current[Hotkeys[i]].wasPressedThisFrame)
                continue;

            TeleportToSector(i);
            return;
        }
#endif
    }

    private void TeleportToSector(int index)
    {
        // Во время Vault, Ledge Climb и подката не телепортируемся,
        // чтобы не нарушить управление KCC и восстановление коллизий.
        if ((traversalMotor != null && traversalMotor.IsTraversing) ||
            (playerMovement != null && playerMovement.IsSliding))
            return;

        if (index >= transform.childCount)
        {
            Debug.LogWarning($"Teleport point F{index + 1} not configured.");
            return;
        }

        Transform target = transform.GetChild(index);

        // Сбрасываем инерцию и переносим капсулу персонажа.
        playerMotor.BaseVelocity = Vector3.zero;
        playerMotor.SetPosition(target.position);

        // Сбрасываем старое состояние контакта с землёй.
        playerMotor.ForceUnground(0.05f);
    }
}