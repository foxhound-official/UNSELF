using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonitorBootTransition : MonoBehaviour
{
    [Header("Boot UI")]
    [SerializeField] private GameObject bootLayout;
    [SerializeField] private GameObject background;

    [Header("Monitor Transition")]
    [SerializeField] private GameObject transitionRoot;
    [SerializeField] private RectTransform topShutter;
    [SerializeField] private RectTransform bottomShutter;
    [SerializeField] private Image scanLine;

    [Header("Flicker")]
    [SerializeField] private int flickerCount = 2;
    [SerializeField] private float flickerOffDuration = 0.05f;
    [SerializeField] private float flickerOnDuration = 0.07f;

    [Header("Screen Opening")]
    [SerializeField] private float scanLineHoldDuration = 0.1f;
    [SerializeField] private float openDuration = 0.55f;
    [SerializeField] private float scanLineFadeDuration = 0.18f;

    [Header("Interference")]
    [SerializeField] private float scanLineJitter = 2f;
    [SerializeField] private float minimumScanLineAlpha = 0.35f;

    private Vector2 scanLineStartPosition;
    private Color scanLineBaseColor;

    private void Awake()
    {
        scanLineStartPosition =
            scanLine.rectTransform.anchoredPosition;

        scanLineBaseColor =
            scanLine.color;

        transitionRoot.SetActive(false);
    }

    public IEnumerator Play()
    {
        yield return FlickerBootScreen();

        PrepareMonitorTransition();

        yield return FlickerScanLine();

        yield return OpenScreen();
    }

    private IEnumerator FlickerBootScreen()
    {
        for (int i = 0; i < flickerCount; i++)
        {
            bootLayout.SetActive(false);

            yield return new WaitForSeconds(
                flickerOffDuration
            );

            bootLayout.SetActive(true);

            yield return new WaitForSeconds(
                flickerOnDuration
            );
        }
    }

    private void PrepareMonitorTransition()
    {
        transitionRoot.SetActive(true);

        topShutter.localScale =
            Vector3.one;

        bottomShutter.localScale =
            Vector3.one;

        scanLine.rectTransform.anchoredPosition =
            scanLineStartPosition;

        SetScanLineAlpha(1f);

        background.SetActive(false);
        bootLayout.SetActive(false);
    }

    private IEnumerator FlickerScanLine()
    {
        float elapsed = 0f;

        while (elapsed < scanLineHoldDuration)
        {
            elapsed += Time.deltaTime;

            float alpha =
                Random.Range(
                    minimumScanLineAlpha,
                    1f
                );

            float verticalJitter =
                Random.Range(
                    -scanLineJitter,
                    scanLineJitter
                );

            scanLine.rectTransform.anchoredPosition =
                scanLineStartPosition +
                Vector2.up * verticalJitter;

            SetScanLineAlpha(alpha);

            yield return null;
        }

        scanLine.rectTransform.anchoredPosition =
            scanLineStartPosition;

        SetScanLineAlpha(1f);
    }

    private IEnumerator OpenScreen()
    {
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / openDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            float shutterScale =
                Mathf.Lerp(
                    1f,
                    0f,
                    smoothProgress
                );

            topShutter.localScale =
                new Vector3(
                    1f,
                    shutterScale,
                    1f
                );

            bottomShutter.localScale =
                new Vector3(
                    1f,
                    shutterScale,
                    1f
                );

            UpdateScanLine(elapsed);

            yield return null;
        }

        topShutter.localScale =
            new Vector3(1f, 0f, 1f);

        bottomShutter.localScale =
            new Vector3(1f, 0f, 1f);

        SetScanLineAlpha(0f);
    }

    private void UpdateScanLine(float elapsed)
    {
        float fadeProgress =
            Mathf.Clamp01(
                elapsed / scanLineFadeDuration
            );

        float interference =
            Random.Range(
                minimumScanLineAlpha,
                1f
            );

        float alpha =
            (1f - fadeProgress) *
            interference;

        SetScanLineAlpha(alpha);

        float verticalJitter =
            Random.Range(
                -scanLineJitter,
                scanLineJitter
            );

        scanLine.rectTransform.anchoredPosition =
            scanLineStartPosition +
            Vector2.up * verticalJitter;
    }

    private void SetScanLineAlpha(float alpha)
    {
        Color color = scanLineBaseColor;

        color.a =
            scanLineBaseColor.a *
            alpha;

        scanLine.color = color;
    }
}