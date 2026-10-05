using System;
using System.Collections;
using UnityEngine;

public class MemberMenuTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private RectTransform leftPerspective;
    [SerializeField] private RectTransform rightPerspective;

    [Header("Open")]
    [SerializeField] private float openDuration = 0.4f;

    [Header("Close")]
    [SerializeField] private float closeDuration = 0.25f;

    [Header("Movement")]
    [SerializeField] private float offset = 90f;

    private Vector2 leftTargetPosition;
    private Vector2 rightTargetPosition;

    private void Awake()
    {
        leftTargetPosition =
            leftPerspective.anchoredPosition;

        rightTargetPosition =
            rightPerspective.anchoredPosition;
    }

    public void PlayOpen()
    {
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    public void PlayClose(Action onComplete)
    {
        StopAllCoroutines();
        StartCoroutine(CloseRoutine(onComplete));
    }

    private IEnumerator OpenRoutine()
    {
        Vector2 leftStartPosition =
            leftTargetPosition +
            Vector2.left * offset;

        Vector2 rightStartPosition =
            rightTargetPosition +
            Vector2.right * offset;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        leftPerspective.anchoredPosition =
            leftStartPosition;

        rightPerspective.anchoredPosition =
            rightStartPosition;

        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / openDuration
                );

            float easedT =
                SmoothStep(t);

            canvasGroup.alpha = easedT;

            leftPerspective.anchoredPosition =
                Vector2.Lerp(
                    leftStartPosition,
                    leftTargetPosition,
                    easedT
                );

            rightPerspective.anchoredPosition =
                Vector2.Lerp(
                    rightStartPosition,
                    rightTargetPosition,
                    easedT
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        leftPerspective.anchoredPosition =
            leftTargetPosition;

        rightPerspective.anchoredPosition =
            rightTargetPosition;
    }

    private IEnumerator CloseRoutine(
        Action onComplete)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Vector2 leftStartPosition =
            leftPerspective.anchoredPosition;

        Vector2 rightStartPosition =
            rightPerspective.anchoredPosition;

        Vector2 leftEndPosition =
            leftTargetPosition +
            Vector2.left * offset;

        Vector2 rightEndPosition =
            rightTargetPosition +
            Vector2.right * offset;

        float startAlpha =
            canvasGroup.alpha;

        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / closeDuration
                );

            float easedT =
                SmoothStep(t);

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    easedT
                );

            leftPerspective.anchoredPosition =
                Vector2.Lerp(
                    leftStartPosition,
                    leftEndPosition,
                    easedT
                );

            rightPerspective.anchoredPosition =
                Vector2.Lerp(
                    rightStartPosition,
                    rightEndPosition,
                    easedT
                );

            yield return null;
        }

        canvasGroup.alpha = 0f;

        leftPerspective.anchoredPosition =
            leftEndPosition;

        rightPerspective.anchoredPosition =
            rightEndPosition;

        onComplete?.Invoke();
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}