using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMarkerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private RectTransform markerRoot;
    [SerializeField] private TMP_Text markerTemplate;

    [Header("Animation")]
    [SerializeField, Min(0.01f)]
    private float fadeInDuration = 0.2f;

    [SerializeField, Min(0.01f)]
    private float fadeOutDuration = 0.7f;

    [SerializeField]
    private float markerVerticalOffset = 42f;

    [SerializeField, Min(0f)]
    private float appearSlideDistance = 9f;

    private sealed class MarkerState
    {
        public TMP_Text label;
        public CanvasGroup canvasGroup;
        public Image[] accents;

        public float expirationTime;
        public float alpha;
        public float appearProgress;
    }

    private readonly Dictionary<WorldMarkerTarget, MarkerState>
        activeMarkers = new();

    private readonly List<WorldMarkerTarget>
        markersToRemove = new();

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (markerRoot == null)
            markerRoot = GetComponent<RectTransform>();

        if (markerTemplate != null)
            markerTemplate.gameObject.SetActive(false);
    }

    public void Reveal(
        WorldMarkerTarget target,
        MemberDefinition sourceMember)
    {
        if (target == null ||
            !target.isActiveAndEnabled ||
            markerRoot == null ||
            markerTemplate == null)
        {
            return;
        }

        if (!activeMarkers.TryGetValue(
            target,
            out MarkerState state))
        {
            TMP_Text label = Instantiate(
                markerTemplate,
                markerRoot
            );

            label.gameObject.name =
                $"Marker_{target.name}";

            CanvasGroup canvasGroup =
                label.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup =
                    label.gameObject.AddComponent<CanvasGroup>();

            state = new MarkerState
            {
                label = label,
                canvasGroup = canvasGroup,
                accents = label.GetComponentsInChildren<Image>(true),
                alpha = 0f,
                appearProgress = 0f
            };

            label.alpha = 1f;
            canvasGroup.alpha = 0f;

            label.gameObject.SetActive(true);

            activeMarkers.Add(target, state);
        }

        state.label.text = target.MarkerText;

        Color markerColor = sourceMember != null
            ? MemberClassColors.Get(sourceMember.Class)
            : markerTemplate.color;

        SetMarkerColor(state, markerColor);

        state.expirationTime =
            Time.time + target.PersistenceSeconds;
    }

    public void ClearAll()
    {
        foreach (MarkerState state in activeMarkers.Values)
        {
            // Запускаем штатное исчезновение метки.
            state.expirationTime = Time.time;
        }
    }

    private static void SetMarkerColor(
        MarkerState state,
        Color color)
    {
        state.label.color = color;

        foreach (Image accent in state.accents)
        {
            if (accent == null)
                continue;

            // Сохраняем индивидуальную прозрачность
            // декоративных элементов.
            Color accentColor = color;
            accentColor.a = accent.color.a;

            accent.color = accentColor;
        }
    }

    private void LateUpdate()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || markerRoot == null)
            return;

        markersToRemove.Clear();

        foreach (var pair in activeMarkers)
        {
            WorldMarkerTarget target = pair.Key;
            MarkerState state = pair.Value;

            if (target == null ||
                !target.isActiveAndEnabled ||
                state.label == null)
            {
                if (state.label != null)
                    Destroy(state.label.gameObject);

                markersToRemove.Add(target);
                continue;
            }

            bool isActive =
                Time.time < state.expirationTime;

            float fadeDuration = isActive
                ? fadeInDuration
                : fadeOutDuration;

            float targetAlpha = isActive ? 1f : 0f;

            state.alpha = Mathf.MoveTowards(
                state.alpha,
                targetAlpha,
                Time.deltaTime / fadeDuration
            );

            // Анимация первого появления.
            state.appearProgress = Mathf.MoveTowards(
                state.appearProgress,
                1f,
                Time.deltaTime / fadeInDuration
            );

            Vector3 viewportPosition =
                playerCamera.WorldToViewportPoint(
                    target.WorldPosition
                );

            bool isOnScreen =
                viewportPosition.z > 0f &&
                viewportPosition.x >= 0f &&
                viewportPosition.x <= 1f &&
                viewportPosition.y >= 0f &&
                viewportPosition.y <= 1f;

            if (isOnScreen)
            {
                Vector3 screenPosition =
                    playerCamera.ViewportToScreenPoint(
                        viewportPosition
                    );

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    markerRoot,
                    screenPosition,
                    null,
                    out Vector2 localPosition
                );

                float easedProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    state.appearProgress
                );

                float slideOffset =
                    appearSlideDistance *
                    (1f - easedProgress);

                state.label.rectTransform.anchoredPosition =
                    localPosition +
                    Vector2.up *
                    (markerVerticalOffset - slideOffset);
            }

            // CanvasGroup влияет на текст, рамку и линию.
            state.canvasGroup.alpha =
                isOnScreen ? state.alpha : 0f;

            if (!isActive && state.alpha <= 0.001f)
            {
                Destroy(state.label.gameObject);
                markersToRemove.Add(target);
            }
        }

        foreach (WorldMarkerTarget target in markersToRemove)
            activeMarkers.Remove(target);
    }
}