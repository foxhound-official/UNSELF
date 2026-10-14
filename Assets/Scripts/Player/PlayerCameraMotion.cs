using UnityEngine;

public class PlayerCameraMotion : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Walking")]
    [SerializeField] private float walkBobFrequency = 7.5f;
    [SerializeField] private float sprintBobFrequency = 10f;
    [SerializeField] private float horizontalBobAmplitude = 0.012f;
    [SerializeField] private float verticalBobAmplitude = 0.018f;
    [SerializeField] private float bobReturnSpeed = 10f;

    [Header("Landing")]
    [SerializeField] private float landingImpactThreshold = 4f;
    [SerializeField] private float landingImpactScale = 0.012f;
    [SerializeField] private float maximumLandingDip = 0.1f;
    [SerializeField] private float landingRecoverySpeed = 9f;

    private Vector3 baseLocalPosition;
    private Vector3 currentBobOffset;
    private float bobPhase;
    private float landingOffset;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        playerMovement.Landed += HandleLanding;
    }

    private void OnDisable()
    {
        playerMovement.Landed -= HandleLanding;
    }

    private void Update()
    {
        UpdateBob();
        UpdateLanding();

        transform.localPosition =
            baseLocalPosition +
            currentBobOffset +
            Vector3.up * landingOffset;
    }

    private void UpdateBob()
    {
        bool isMoving =
            playerMovement.IsGrounded &&
            playerMovement.HorizontalSpeed > 0.1f;

        Vector3 targetOffset = Vector3.zero;

        if (isMoving)
        {
            float frequency = Mathf.Lerp(
                walkBobFrequency,
                sprintBobFrequency,
                playerMovement.SpeedRatio
            );

            bobPhase += frequency * Time.deltaTime;

            float intensity = Mathf.Lerp(
                0.65f,
                1f,
                playerMovement.SpeedRatio
            );

            float horizontalOffset =
                Mathf.Sin(bobPhase) *
                horizontalBobAmplitude *
                intensity;

            float verticalOffset =
                Mathf.Sin(bobPhase * 2f) *
                verticalBobAmplitude *
                intensity;

            targetOffset = new Vector3(
                horizontalOffset,
                verticalOffset,
                0f
            );
        }

        float blend = 1f - Mathf.Exp(-bobReturnSpeed * Time.deltaTime);

        currentBobOffset = Vector3.Lerp(
            currentBobOffset,
            targetOffset,
            blend
        );
    }

    private void UpdateLanding()
    {
        float blend =
            1f -
            Mathf.Exp(-landingRecoverySpeed * Time.deltaTime);

        landingOffset = Mathf.Lerp(
            landingOffset,
            0f,
            blend
        );
    }

    private void HandleLanding(float impactSpeed)
    {
        float impact =
            Mathf.Max(
                0f,
                impactSpeed - landingImpactThreshold
            );

        landingOffset = -Mathf.Min(
            impact * landingImpactScale,
            maximumLandingDip
        );
    }
}