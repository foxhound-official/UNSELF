using System.Collections;
using UnityEngine;

public class SimpleDoor : MonoBehaviour
{
    [SerializeField] private bool isLocked = true;
    [SerializeField] private Vector3 openOffset = new Vector3(2.5f, 0f, 0f);
    [SerializeField] private float openDuration = 1f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen;
    private bool isMoving;

    public bool IsLocked => isLocked;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition + openOffset;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void Open()
    {
        if (isLocked || isOpen || isMoving)
            return;

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isMoving = true;

        Vector3 startPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / openDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.localPosition = Vector3.Lerp(
                startPosition,
                openPosition,
                smoothProgress
            );

            yield return null;
        }

        transform.localPosition = openPosition;

        isOpen = true;
        isMoving = false;
    }
}