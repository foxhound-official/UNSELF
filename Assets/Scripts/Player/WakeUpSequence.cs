using System.Collections;
using UnityEngine;

public class WakeUpSequence : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float startHeightOffset = 0.35f;
    [SerializeField] private float startLookDown = 55f;

    private Vector3 standingPosition;
    private Quaternion standingRotation;

    private void Awake()
    {
        standingPosition = cameraTransform.localPosition;
        standingRotation = cameraTransform.localRotation;

        playerLook.enabled = false;
        playerMovement.enabled = false;
    }

    public void Begin()
    {
        StartCoroutine(WakeUp());
    }

    private IEnumerator WakeUp()
    {
        Vector3 startPosition =
            standingPosition + Vector3.down * startHeightOffset;

        Quaternion startRotation =
            Quaternion.Euler(startLookDown, 0f, 0f);

        cameraTransform.localPosition = startPosition;
        cameraTransform.localRotation = startRotation;

        float elapsed = 0f;

        // Stage 1: camera rises
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            cameraTransform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    standingPosition,
                    smoothProgress
                );

            cameraTransform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    standingRotation,
                    smoothProgress
                );

            yield return null;
        }

        cameraTransform.localPosition = standingPosition;
        cameraTransform.localRotation = standingRotation;

        // Stage 2: slowly look around
        Quaternion lookLeft =
            Quaternion.Euler(-5f, -12f, 0f);

        Quaternion lookRight =
            Quaternion.Euler(-3f, 10f, 0f);

        yield return RotateCamera(standingRotation, lookLeft, 0.6f);
        yield return RotateCamera(lookLeft, lookRight, 1.0f);
        yield return RotateCamera(lookRight, standingRotation, 0.7f);

        playerLook.ResetRotation();
        playerLook.enabled = true;
        playerMovement.enabled = true;
    }

    private IEnumerator RotateCamera(Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            cameraTransform.localRotation =
                Quaternion.Slerp(
                    from,
                    to,
                    smoothProgress
                );

            yield return null;
        }

        cameraTransform.localRotation = to;
    }
}