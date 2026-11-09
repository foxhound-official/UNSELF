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
    [SerializeField] private RectTransform boundsMarkerTemplate;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float clearFadeOutDuration = 0.12f;
    [SerializeField] private float markerVerticalOffset = 42f;
    [SerializeField, Min(0f)] private float appearSlideDistance = 9f;

    [Header("Bounds Marker")]
    [SerializeField, Min(0f)] private float boundsPadding = 12f;
    [SerializeField, Min(1f)] private float minBoundsWidth = 72f;
    [SerializeField, Min(1f)] private float minBoundsHeight = 72f;
    [SerializeField, Min(1f)] private float maxBoundsWidth = 360f;
    [SerializeField, Min(1f)] private float maxBoundsHeight = 300f;
    [SerializeField, Min(0.1f)] private float boundsFollowSpeed = 18f;

    private sealed class MarkerState
    {
        public RectTransform rect;
        public TMP_Text label;
        public CanvasGroup canvasGroup;
        public Image[] accents;
        public bool isBounds;
        public bool isClearing;
        public bool boundsInitialized;
        public Vector2 boundsCenter;
        public Vector2 boundsSize;
        public float expirationTime;
        public float alpha;
        public float appearProgress;
    }

    private readonly Dictionary<WorldMarkerTarget, MarkerState> activeMarkers = new();
    private readonly List<WorldMarkerTarget> markersToRemove = new();
    private readonly Vector3[] boundsCorners = new Vector3[8];
    private readonly float[] cornerDepths = new float[8];

    // Edges of a box whose corners are indexed by the X/Y/Z sign bits.
    private static readonly int[] BoundsEdges =
    {
        0, 1, 2, 3, 4, 5, 6, 7,
        0, 2, 1, 3, 4, 6, 5, 7,
        0, 4, 1, 5, 2, 6, 3, 7
    };

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (markerRoot == null)
            markerRoot = GetComponent<RectTransform>();

        if (markerTemplate != null)
            markerTemplate.gameObject.SetActive(false);

        if (boundsMarkerTemplate != null)
            boundsMarkerTemplate.gameObject.SetActive(false);
    }

    public void Reveal(WorldMarkerTarget target, MemberDefinition sourceMember)
    {
        if (target == null || !target.isActiveAndEnabled ||
            markerRoot == null || markerTemplate == null)
        {
            return;
        }

        bool canUseBounds = boundsMarkerTemplate != null &&
                            target.TryGetWorldBounds(out _);

        // Auto can fall back to Point. Explicit Bounds requires a model.
        if (target.DisplayMode == WorldMarkerDisplayMode.Bounds && !canUseBounds)
            return;

        bool useBounds = target.DisplayMode != WorldMarkerDisplayMode.Point &&
                         canUseBounds;

        activeMarkers.TryGetValue(target, out MarkerState state);

        // If presentation changes at runtime, recreate the visual only.
        if (state != null && state.isBounds != useBounds)
        {
            if (state.rect != null)
                Destroy(state.rect.gameObject);

            activeMarkers.Remove(target);
            state = null;
        }

        if (state == null)
        {
            RectTransform rect;
            TMP_Text label;

            if (useBounds)
            {
                rect = Instantiate(boundsMarkerTemplate, markerRoot);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                label = rect.GetComponentInChildren<TMP_Text>(true);
            }
            else
            {
                label = Instantiate(markerTemplate, markerRoot);
                rect = label.rectTransform;
            }

            rect.gameObject.name = $"Marker_{target.name}";

            CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();

            state = new MarkerState
            {
                rect = rect,
                label = label,
                canvasGroup = canvasGroup,
                accents = rect.GetComponentsInChildren<Image>(true),
                isBounds = useBounds,
                alpha = 0f,
                appearProgress = 0f
            };

            if (label != null)
                label.alpha = 1f;

            canvasGroup.alpha = 0f;
            rect.gameObject.SetActive(true);
            activeMarkers.Add(target, state);
        }

        if (state.label != null)
            state.label.text = target.MarkerText;

        Color markerColor = sourceMember != null
            ? MemberClassColors.Get(sourceMember.Class)
            : markerTemplate.color;

        SetMarkerColor(state, markerColor);

        state.expirationTime = Time.time + target.PersistenceSeconds;
        state.isClearing = false;
    }

    public void ClearAll()
    {
        foreach (MarkerState state in activeMarkers.Values)
        {
            state.expirationTime = Time.time;
            state.isClearing = true;
        }
    }

    private static void SetMarkerColor(MarkerState state, Color color)
    {
        if (state.label != null)
            state.label.color = color;

        foreach (Image accent in state.accents)
        {
            if (accent == null)
                continue;

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

        foreach (KeyValuePair<WorldMarkerTarget, MarkerState> pair in activeMarkers)
        {
            WorldMarkerTarget target = pair.Key;
            MarkerState state = pair.Value;

            if (target == null || !target.isActiveAndEnabled || state.rect == null)
            {
                if (state.rect != null)
                    Destroy(state.rect.gameObject);

                markersToRemove.Add(target);
                continue;
            }

            bool isActive = Time.time < state.expirationTime;
            float fadeDuration = isActive
                ? fadeInDuration
                : state.isClearing ? clearFadeOutDuration : fadeOutDuration;

            state.alpha = Mathf.MoveTowards(
                state.alpha, isActive ? 1f : 0f, Time.deltaTime / fadeDuration);

            state.appearProgress = Mathf.MoveTowards(
                state.appearProgress, 1f, Time.deltaTime / fadeInDuration);

            bool onScreen = state.isBounds
                ? UpdateBoundsMarker(target, state)
                : UpdatePointMarker(target, state);

            state.canvasGroup.alpha = onScreen ? state.alpha : 0f;

            if (!isActive && state.alpha <= 0.001f)
            {
                Destroy(state.rect.gameObject);
                markersToRemove.Add(target);
            }
        }

        foreach (WorldMarkerTarget target in markersToRemove)
            activeMarkers.Remove(target);
    }

    private bool UpdatePointMarker(WorldMarkerTarget target, MarkerState state)
    {
        Vector3 viewportPosition = playerCamera.WorldToViewportPoint(target.WorldPosition);
        if (viewportPosition.z <= 0f || viewportPosition.x < 0f ||
            viewportPosition.x > 1f || viewportPosition.y < 0f ||
            viewportPosition.y > 1f)
        {
            return false;
        }

        Vector3 screenPosition = playerCamera.ViewportToScreenPoint(viewportPosition);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            markerRoot, screenPosition, null, out Vector2 localPosition))
        {
            return false;
        }

        float easedProgress = Mathf.SmoothStep(0f, 1f, state.appearProgress);
        float slideOffset = appearSlideDistance * (1f - easedProgress);

        state.rect.anchoredPosition = localPosition +
            Vector2.up * (markerVerticalOffset - slideOffset);

        return true;
    }

    private bool UpdateBoundsMarker(WorldMarkerTarget target, MarkerState state)
    {
        if (!target.TryGetWorldBounds(out Bounds worldBounds) ||
            !TryProjectBounds(worldBounds, out Vector2 projectedCenter,
                out Vector2 projectedSize))
        {
            state.boundsInitialized = false;
            return false;
        }

        Vector2 size = new Vector2(
            Mathf.Clamp(projectedSize.x + boundsPadding * 2f,
                minBoundsWidth, Mathf.Max(minBoundsWidth, maxBoundsWidth)),
            Mathf.Clamp(projectedSize.y + boundsPadding * 2f,
                minBoundsHeight, Mathf.Max(minBoundsHeight, maxBoundsHeight))
        );

        if (!state.boundsInitialized)
        {
            state.boundsCenter = projectedCenter;
            state.boundsSize = size;
            state.boundsInitialized = true;
        }
        else
        {
            float t = 1f - Mathf.Exp(-boundsFollowSpeed * Time.deltaTime);
            state.boundsCenter = Vector2.Lerp(state.boundsCenter, projectedCenter, t);
            state.boundsSize = Vector2.Lerp(state.boundsSize, size, t);
        }

        state.rect.anchoredPosition = state.boundsCenter;
        state.rect.sizeDelta = state.boundsSize;
        return true;
    }

    private bool TryProjectBounds(Bounds worldBounds,
        out Vector2 center, out Vector2 size)
    {
        center = default;
        size = default;

        Vector3 bCenter = worldBounds.center;
        Vector3 ext = worldBounds.extents;
        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;
        float near = Mathf.Max(playerCamera.nearClipPlane, 0.01f) + 0.001f;

        Vector2 minViewport = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maxViewport = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bCenter + new Vector3(
                (i & 1) == 0 ? -ext.x : ext.x,
                (i & 2) == 0 ? -ext.y : ext.y,
                (i & 4) == 0 ? -ext.z : ext.z
            );

            boundsCorners[i] = corner;
            cornerDepths[i] = Vector3.Dot(corner - cameraPosition, cameraForward);

            if (cornerDepths[i] >= near)
                IncludeViewportPoint(corner, ref minViewport, ref maxViewport);
        }

        // Intersect bounding-box edges with the camera near plane.
        // This avoids huge/inverted rectangles when a model is very close.
        for (int i = 0; i < BoundsEdges.Length; i += 2)
        {
            int a = BoundsEdges[i];
            int b = BoundsEdges[i + 1];
            bool aInFront = cornerDepths[a] >= near;
            bool bInFront = cornerDepths[b] >= near;

            if (aInFront == bInFront)
                continue;

            float t = (near - cornerDepths[a]) /
                (cornerDepths[b] - cornerDepths[a]);

            Vector3 intersection = Vector3.LerpUnclamped(
                boundsCorners[a], boundsCorners[b], t);
            IncludeViewportPoint(intersection, ref minViewport, ref maxViewport);
        }

        if (float.IsInfinity(minViewport.x) ||
            maxViewport.x <= 0f || minViewport.x >= 1f ||
            maxViewport.y <= 0f || minViewport.y >= 1f)
        {
            return false;
        }

        // Clip the projected rectangle to the camera viewport.
        minViewport = Vector2.Max(minViewport, Vector2.zero);
        maxViewport = Vector2.Min(maxViewport, Vector2.one);

        Vector2 minScreen = playerCamera.ViewportToScreenPoint(minViewport);
        Vector2 maxScreen = playerCamera.ViewportToScreenPoint(maxViewport);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                markerRoot, minScreen, null, out Vector2 minLocal) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                markerRoot, maxScreen, null, out Vector2 maxLocal))
        {
            return false;
        }

        center = (minLocal + maxLocal) * 0.5f;
        size = maxLocal - minLocal;
        return true;
    }

    private void IncludeViewportPoint(Vector3 worldPoint,
        ref Vector2 minViewport, ref Vector2 maxViewport)
    {
        Vector3 projected = playerCamera.WorldToViewportPoint(worldPoint);
        Vector2 p = new Vector2(projected.x, projected.y);
        minViewport = Vector2.Min(minViewport, p);
        maxViewport = Vector2.Max(maxViewport, p);
    }
}