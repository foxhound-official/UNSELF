using System;
using System.Collections;
using KinematicCharacterController;
using UnityEngine;

/// <summary>
/// Выполняет автоматические Vault и Ledge Climb по проверенным траекториям, временно управляя KCC.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TraversalMotor : MonoBehaviour
{
    private const float DirectionThresholdSqr = 0.0001f;
    private const float LandingSurfaceOffset = 0.02f;
    private const float MinimumPathDistance = 0.001f;

    [SerializeField] private TraversalProfile traversalProfile;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private TraversalDetector traversalDetector;
    [SerializeField] private TraversalResolver traversalResolver;
    [SerializeField] private TraversalPathValidator pathValidator;

    [Header("Enhanced Mobility")]
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability enhancedMobilityCapability;

    private KinematicCharacterMotor motor;
    private PlayerInputReader inputReader;
    private LedgeGrabDetector ledgeGrabDetector;

    private Coroutine traversalRoutine;
    private float ledgeClimbCooldownUntil;

    private bool HasEnhancedMobility => memberAssembly != null && enhancedMobilityCapability != null &&
        memberAssembly.HasCapability(enhancedMobilityCapability);

    public bool IsTraversing => traversalRoutine != null;

    public event Action<TraversalType> TraversalStarted;

    // Получаем мотор, источник ввода и детектор захвата уступа.
    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        inputReader = GetComponent<PlayerInputReader>();
        ledgeGrabDetector = GetComponent<LedgeGrabDetector>();
    }

    private void Update()
    {
        // Подтягивание автоматически запускается при удержании прыжка рядом с уступом.
        TryStartAirborneLedgeClimb();
    }

    // Vault доступен с земли, если перед игроком есть подходящее препятствие и место для приземления.
    public bool TryStartTraversal()
    {
        if (IsTraversing)
            return false;

        if (motor == null || playerMovement == null || traversalDetector == null ||
            traversalResolver == null || pathValidator == null)
            return false;

        if (playerMovement.MovementMode != PlayerMovementMode.Locomotion)
            return false;

        if (!playerMovement.IsGrounded || playerMovement.IsCrouching || playerMovement.IsSliding)
            return false;

        if (!traversalDetector.TryDetect(out TraversalCandidate candidate))
            return false;

        TraversalType traversalType = traversalResolver.Resolve(candidate);

        if (traversalType != TraversalType.Vault)
            return false;

        return TryStartVault(candidate, traversalResolver.LandingPoint);
    }

    // Подтягивание доступно только в воздухе при удержании прыжка и вне кулдауна.
    private void TryStartAirborneLedgeClimb()
    {
        if (IsTraversing || traversalProfile == null || ledgeGrabDetector == null || inputReader == null)
            return;

        if (Time.time < ledgeClimbCooldownUntil)
            return;

        if (!inputReader.JumpHeld)
            return;

        if (playerMovement.MovementMode != PlayerMovementMode.Locomotion)
            return;

        // Захват запрещён на земле и во время подката, но разрешён при удержании Ctrl в воздухе.
        if (playerMovement.IsGrounded || playerMovement.IsSliding)
            return;

        if (playerMovement.VerticalVelocity > traversalProfile.LedgeClimbMaximumUpwardSpeed)
            return;

        if (!ledgeGrabDetector.TryDetect(out LedgeGrabCandidate candidate))
            return;

        TryStartLedgeClimb(candidate);
    }

    // Сначала рассчитываем точки захвата и подъёма, затем проверяем весь путь капсулы.
    private bool TryStartLedgeClimb(LedgeGrabCandidate candidate)
    {
        Vector3 up = motor.CharacterUp;
        Vector3 wallNormal = Vector3.ProjectOnPlane(candidate.WallNormal, up).normalized;

        if (wallNormal.sqrMagnitude <= DirectionThresholdSqr)
            return false;

        Vector3 traversalDirection = -wallNormal;
        Vector3 startPoint = motor.TransientPosition;

        // Точки для захвата края, подъёма корпуса и выхода на верхнюю поверхность.

        float wallOffset = motor.Capsule.radius + traversalProfile.LedgeClimbWallClearance;

        Vector3 catchPoint = candidate.TopPoint + wallNormal * wallOffset - up * traversalProfile.LedgeClimbHangDepth;

        Vector3 pullPoint = candidate.TopPoint + wallNormal * wallOffset + up * traversalProfile.LedgeClimbTopClearance;

        Vector3 crestPoint = candidate.TopPoint + traversalDirection * traversalProfile.LedgeClimbCrestForward +
            up * traversalProfile.LedgeClimbTopClearance;

        Vector3 landingPoint = candidate.TopPoint + traversalDirection * traversalProfile.LedgeClimbLandingForward +
            up * LandingSurfaceOffset;

        // Проверяем каждый отрезок пути, чтобы не проходить сквозь постороннюю геометрию.
        if (!pathValidator.IsSegmentClear(startPoint, catchPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(catchPoint, pullPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(pullPoint, crestPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(crestPoint, landingPoint, candidate.Collider))
            return false;

        TraversalStarted?.Invoke(TraversalType.LedgeClimb);

        traversalRoutine = StartCoroutine(LedgeClimb(catchPoint, pullPoint, crestPoint, landingPoint, traversalDirection));

        return true;
    }

    // Отдельно проверяем вход, верхнюю точку и безопасный выход с препятствия.
    private bool TryStartVault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        Vector3 startPoint = motor.TransientPosition;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        Vector3 entryPoint = candidate.ObstaclePoint +
            candidate.ObstacleNormal * (motor.Capsule.radius + traversalProfile.VaultEntryClearance);

        entryPoint.y = startPoint.y + traversalProfile.VaultEntryRise;

        Vector3 clearancePoint = candidate.TopPoint + traversalDirection * traversalProfile.VaultForwardClearance;

        clearancePoint.y = candidate.TopPoint.y + traversalProfile.VaultTopClearance;

        Vector3 finalLandingPoint = landingPoint + Vector3.up * LandingSurfaceOffset;

        // Vault запускается только если все три участка траектории свободны.
        if (!pathValidator.IsSegmentClear(startPoint, entryPoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(entryPoint, clearancePoint, candidate.Collider))
            return false;

        if (!pathValidator.IsSegmentClear(clearancePoint, finalLandingPoint, candidate.Collider))
            return false;

        TraversalStarted?.Invoke(TraversalType.Vault);
        traversalRoutine = StartCoroutine(Vault(candidate, landingPoint));

        return true;
    }

    // Корутинная траектория захвата и подтягивания; обычное движение временно приостановлено.
    private IEnumerator LedgeClimb(Vector3 catchPoint, Vector3 pullPoint, Vector3 crestPoint, Vector3 landingPoint,
        Vector3 traversalDirection)
    {
        float entrySpeed = playerMovement.HorizontalSpeed;

        // Пока идёт управляемое перемещение, обычная физика передвижения приостановлена.
        playerMovement.SetMovementMode(PlayerMovementMode.Traversal);
        BeginAuthoredTraversal();

        try
        {
            Vector3 startPoint = motor.TransientPosition;

            float catchElapsed = 0f;
            float catchDuration = Mathf.Max(0.01f, traversalProfile.LedgeClimbCatchDuration);

            while (catchElapsed < catchDuration)
            {
                catchElapsed += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(catchElapsed / catchDuration);
                float progress = traversalProfile.LedgeClimbProgress.Evaluate(normalizedTime);

                motor.SetPosition(Vector3.Lerp(startPoint, catchPoint, progress));

                yield return null;
            }

            motor.SetPosition(catchPoint);

            float pullDistance = Vector3.Distance(catchPoint, pullPoint);
            float crestDistance = Vector3.Distance(pullPoint, crestPoint);
            float landingDistance = Vector3.Distance(crestPoint, landingPoint);

            float totalDistance = Mathf.Max(MinimumPathDistance, pullDistance + crestDistance + landingDistance);

            float pullEnd = pullDistance / totalDistance;
            float crestEnd = (pullDistance + crestDistance) / totalDistance;

            float pullElapsed = 0f;
            float pullDuration = Mathf.Max(0.01f, traversalProfile.LedgeClimbPullDuration);

            while (pullElapsed < pullDuration)
            {
                pullElapsed += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(pullElapsed / pullDuration);
                float progress = traversalProfile.LedgeClimbProgress.Evaluate(normalizedTime);

                Vector3 targetPosition;

                if (progress < pullEnd)
                {
                    float segmentProgress = pullEnd > MinimumPathDistance ? progress / pullEnd : 1f;

                    targetPosition = Vector3.Lerp(catchPoint, pullPoint, segmentProgress);
                }
                else if (progress < crestEnd)
                {
                    float segmentLength = crestEnd - pullEnd;

                    float segmentProgress = segmentLength > MinimumPathDistance ? (progress - pullEnd) / segmentLength : 1f;

                    targetPosition = Vector3.Lerp(pullPoint, crestPoint, segmentProgress);
                }
                else
                {
                    float segmentLength = 1f - crestEnd;

                    float segmentProgress = segmentLength > MinimumPathDistance ? (progress - crestEnd) / segmentLength
                        : 1f;

                    targetPosition = Vector3.Lerp(crestPoint, landingPoint, segmentProgress);
                }

                // Двигаем капсулу по заранее проверенной траектории.
                motor.SetPosition(targetPosition);

                yield return null;
            }

            motor.SetPosition(landingPoint);
        }
        finally
        {
            // После подтягивания сохраняем только часть входной скорости.
            EndAuthoredTraversal();
            float exitSpeed = entrySpeed * traversalProfile.LedgeClimbExitSpeedMultiplier;
            Vector3 exitVelocity = traversalDirection * exitSpeed;

            // Не даём сразу повторно захватить тот же уступ.
            ledgeClimbCooldownUntil = Time.time + traversalProfile.LedgeClimbCooldown;
            playerMovement.CompleteTraversal(exitVelocity);
            traversalRoutine = null;
        }
    }

    // Корутинная траектория перескока. Коллизии обязательно восстанавливаются в finally.
    private IEnumerator Vault(TraversalCandidate candidate, Vector3 landingPoint)
    {
        float entrySpeed = playerMovement.HorizontalSpeed;
        Vector3 traversalDirection = -candidate.ObstacleNormal;

        playerMovement.SetMovementMode(PlayerMovementMode.Traversal);
        BeginAuthoredTraversal();

        try
        {
            Vector3 startPoint = motor.TransientPosition;

            Vector3 entryPoint = candidate.ObstaclePoint + candidate.ObstacleNormal *
                                 (motor.Capsule.radius + traversalProfile.VaultEntryClearance);

            entryPoint.y = startPoint.y + traversalProfile.VaultEntryRise;

            Vector3 clearancePoint = candidate.TopPoint + traversalDirection * traversalProfile.VaultForwardClearance;

            clearancePoint.y = candidate.TopPoint.y + traversalProfile.VaultTopClearance;

            Vector3 finalLandingPoint = landingPoint + Vector3.up * LandingSurfaceOffset;

            float entryDistance = Vector3.Distance(startPoint, entryPoint);
            float clearanceDistance = Vector3.Distance(entryPoint, clearancePoint);
            float landingDistance = Vector3.Distance(clearancePoint, finalLandingPoint);

            float totalDistance = entryDistance + clearanceDistance + landingDistance;

            // Чем выше скорость входа, тем меньше длительность перескока (в заданных пределах).
            float traversalSpeed = Mathf.Max(entrySpeed, traversalProfile.VaultMinimumTraversalSpeed);

            float duration = Mathf.Clamp(totalDistance / traversalSpeed, traversalProfile.VaultMinimumDuration,
                traversalProfile.VaultMaximumDuration);

            float entryEnd = entryDistance / totalDistance;
            float clearanceEnd = (entryDistance + clearanceDistance) / totalDistance;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float progress = traversalProfile.VaultProgress.Evaluate(normalizedTime);

                Vector3 targetPosition;

                if (progress < entryEnd)
                {
                    float segmentProgress = progress / entryEnd;
                    targetPosition = Vector3.Lerp(startPoint, entryPoint, segmentProgress);
                }
                else if (progress < clearanceEnd)
                {
                    float segmentProgress = (progress - entryEnd) / (clearanceEnd - entryEnd);
                    targetPosition = Vector3.Lerp(entryPoint, clearancePoint, segmentProgress);
                }
                else
                {
                    float segmentProgress = (progress - clearanceEnd) / (1f - clearanceEnd);
                    targetPosition = Vector3.Lerp(clearancePoint, finalLandingPoint, segmentProgress);
                }

                motor.SetPosition(targetPosition);

                yield return null;
            }

            motor.SetPosition(finalLandingPoint);
        }
        finally
        {
            // Обязательно восстанавливаем обработку столкновений перед возвращением управления игроку.
            EndAuthoredTraversal();

            // Сначала возвращаем входную скорость без резкого импульса.
            float exitSpeedBoost = HasEnhancedMobility ? traversalProfile.EnhancedVaultExitSpeedBoost
                : traversalProfile.VaultExitSpeedBoost;
            Vector3 exitDirection = Vector3.ProjectOnPlane(traversalDirection, motor.CharacterUp).normalized;

            playerMovement.CompleteTraversal(exitDirection * entrySpeed);

            // Плавный бонус активируется только после восстановления коллизий и управления KCC.
            playerMovement.BeginVaultExitBoost(exitDirection, entrySpeed, exitSpeedBoost);
            traversalRoutine = null;
        }
    }

    private void BeginAuthoredTraversal()
    {
        // Принимаем управление движением на себя: временно отключаем коллизии и поиск земли KCC.
        motor.BaseVelocity = Vector3.zero;
        motor.SetMovementCollisionsSolvingActivation(false);
        motor.SetGroundSolvingActivation(false);
    }

    private void EndAuthoredTraversal()
    {
        // Должен вызываться при любом завершении Traversal, до CompleteTraversal().
        motor.SetMovementCollisionsSolvingActivation(true);
        motor.SetGroundSolvingActivation(true);
    }
}
