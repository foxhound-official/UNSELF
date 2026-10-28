using UnityEngine;

[CreateAssetMenu(fileName = "Capability", menuName = "UNSELF/Members/Capability")]
public class MemberCapability : ScriptableObject
{
    [SerializeField] private string code;

    [Header("Description")]
    [SerializeField] private string displayName;

    [SerializeField, TextArea(3, 6)]
    private string description;

    public string Code => code;
    public string DisplayName => displayName;
    public string Description => description;
}