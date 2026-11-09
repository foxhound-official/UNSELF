using UnityEngine;

public enum WorldMarkerDisplayMode
{
    Auto = 0,
    Bounds = 1,
    Point = 2
}

public class WorldMarkerTarget : MonoBehaviour
{
    [Header("Marker Identity")]
    [SerializeField] private string markerCode = "T_01";
    [SerializeField] private string markerName = "TERMINAL";

    [Header("Presentation")]
    [SerializeField] private WorldMarkerDisplayMode displayMode = WorldMarkerDisplayMode.Auto;
    [Tooltip("Optional. Leave empty to use renderers on this object and its children.")]
    [SerializeField] private Renderer[] boundsRenderers;

    [Header("Point Marker Position")]
    [SerializeField] private Transform markerAnchor;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Detection")]
    [SerializeField] private MemberCapability requiredCapability;
    [SerializeField] private MemberDefinition sourceMember;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)] private float persistenceSeconds = 5f;

    private Renderer[] cachedRenderers;

    public WorldMarkerDisplayMode DisplayMode => displayMode;
    public string MarkerText => $"{markerCode} // {markerName}";
    public float PersistenceSeconds => persistenceSeconds;

    public Vector3 WorldPosition => markerAnchor != null
        ? markerAnchor.position
        : transform.TransformPoint(localOffset);

    private void Awake()
    {
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        cachedRenderers = boundsRenderers != null && boundsRenderers.Length > 0
            ? boundsRenderers
            : GetComponentsInChildren<Renderer>(true);
    }

    public bool TryGetWorldBounds(out Bounds worldBounds)
    {
        worldBounds = default;

        if (cachedRenderers == null)
            CacheRenderers();

        bool found = false;

        foreach (Renderer item in cachedRenderers)
        {
            if (item == null || !item.enabled || !item.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                worldBounds = item.bounds;
                found = true;
            }
            else
            {
                worldBounds.Encapsulate(item.bounds);
            }
        }

        return found;
    }

    public MemberDefinition ResolveSourceMember(MemberAssembly memberAssembly)
    {
        if (memberAssembly == null)
            return null;

        if (sourceMember != null)
        {
            if (!memberAssembly.HasMember(sourceMember))
                return null;

            if (requiredCapability != null &&
                !sourceMember.HasCapability(requiredCapability))
            {
                return null;
            }

            return sourceMember;
        }

        return memberAssembly.GetFirstMemberWithCapability(requiredCapability);
    }

    public bool CanReveal(MemberAssembly memberAssembly)
    {
        if (requiredCapability == null && sourceMember == null)
            return true;

        return ResolveSourceMember(memberAssembly) != null;
    }
}