
using KinematicCharacterController;
using UnityEngine;

/// <summary>
/// Воспроизводит шаги по фактически пройденному расстоянию.
/// Не зависит от временных FPS-анимаций рук.
/// </summary>
[RequireComponent(typeof(PlayerMovement), typeof(KinematicCharacterMotor))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Step Distance")]
    [SerializeField, Min(0.1f)] private float walkStepDistance = 1.3f;
    [SerializeField, Min(0.1f)] private float sprintStepDistance = 1.8f;
    [SerializeField, Min(0.1f)] private float crouchStepDistance = 1.25f;

    [Header("Step Volume")]
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float sprintVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.35f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float minimumSpeed = 0.1f;
    [SerializeField, Min(0.1f)] private float maximumFrameDistance = 2f;

    [Header("Step Timing")]
    [SerializeField, Min(0f)] private float minimumStepInterval = 0.35f;
    [SerializeField, Min(0f)] private float minimumMovementPerFrame = 0.002f;

    [Header("Pitch Variation")]
    [SerializeField, Range(0f, 0.2f)] private float pitchVariation = 0.06f;

    private float lastStepTime = float.NegativeInfinity;
    private bool wasMoving;

    private PlayerMovement movement;
    private KinematicCharacterMotor motor;

    private Vector3 previousPosition;
    private float accumulatedDistance;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        motor = GetComponent<KinematicCharacterMotor>();
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        accumulatedDistance = 0f;
        lastStepTime = float.NegativeInfinity;
        wasMoving = false;
    }

    private void LateUpdate()
    {
        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;

        // Во время прыжков, подкатов и Traversal обычные шаги не воспроизводятся.
        if (!movement.isActiveAndEnabled ||
            movement.MovementMode != PlayerMovementMode.Locomotion ||
            !movement.IsGrounded ||
            movement.IsSliding)
        {
            accumulatedDistance = 0f;
            wasMoving = false;
            return;
        }

        float distance = Vector3.ProjectOnPlane(displacement, motor.CharacterUp).magnitude;

        // Исключаем телепортации и резкие перемещения.
        if (distance > maximumFrameDistance)
        {
            accumulatedDistance = 0f;
            wasMoving = false;
            return;
        }

        bool isMoving = movement.HorizontalSpeed >= minimumSpeed &&
            distance >= minimumMovementPerFrame;

        if (!isMoving)
        {
            // Короткая остановка не сбрасывает накопленную дистанцию.
            wasMoving = false;
            return;
        }

        // Первый шаг запускаем при начале движения, не дожидаясь полной дистанции.
        if (!wasMoving && Time.time - lastStepTime >= minimumStepInterval)
        {
            PlayFootstep();
            lastStepTime = Time.time;
            accumulatedDistance = 0f;
            wasMoving = true;
            return;
        }

        wasMoving = true;
        accumulatedDistance += distance;

        float stepDistance = movement.IsCrouching ? crouchStepDistance :
            movement.IsSprinting ? sprintStepDistance : walkStepDistance;

        if (accumulatedDistance < stepDistance ||
            Time.time - lastStepTime < minimumStepInterval)
            return;

        accumulatedDistance %= stepDistance;

        PlayFootstep();
        lastStepTime = Time.time;
    }

    private void PlayFootstep()
    {
        // KCC уже знает, с каким коллайдером установлен контакт.
        Collider groundCollider = motor.GroundingStatus.GroundCollider;

        FootstepSurface surface = groundCollider != null
            ? groundCollider.GetComponentInParent<FootstepSurface>()
            : null;

        string soundId = surface != null ? surface.SoundId : "footstep_concrete";

        float volume = movement.IsCrouching ? crouchVolume :
            movement.IsSprinting ? sprintVolume : walkVolume;

        float pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        AudioService.PlayFootstep(soundId, volume, pitch);
    }
}