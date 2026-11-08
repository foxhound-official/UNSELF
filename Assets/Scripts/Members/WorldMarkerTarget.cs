using UnityEngine;

public class WorldMarkerTarget : MonoBehaviour
{
    [Header("Marker Identity")]
    [SerializeField] private string markerCode = "T_01";
    [SerializeField] private string markerName = "TERMINAL";

    [Header("World Position")]
    [SerializeField] private Transform markerAnchor;
    [SerializeField] private Vector3 localOffset =
        new Vector3(0f, 1.5f, 0f);

    [Header("Detection")]
    [SerializeField] private MemberCapability requiredCapability;
    [SerializeField] private MemberDefinition sourceMember;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)]
    private float persistenceSeconds = 5f;

    public string MarkerText =>
        $"{markerCode} // {markerName}";

    public float PersistenceSeconds => persistenceSeconds;

    public Vector3 WorldPosition =>
        markerAnchor != null
            ? markerAnchor.position
            : transform.TransformPoint(localOffset);

    public MemberDefinition ResolveSourceMember(
        MemberAssembly memberAssembly)
    {
        if (memberAssembly == null)
            return null;

        // Если источник указан вручную,
        // проверяем, что MEMBER действительно установлен.
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

        // Автоматически находим MEMBER,
        // предоставляющего необходимую способность.
        return memberAssembly.GetFirstMemberWithCapability(
            requiredCapability
        );
    }

    public bool CanReveal(MemberAssembly memberAssembly)
    {
        // Обычный объект без привязки к MEMBER.
        if (requiredCapability == null && sourceMember == null)
            return true;

        return ResolveSourceMember(memberAssembly) != null;
    }
}