using UnityEngine;

[CreateAssetMenu(fileName = "Capability", menuName = "UNSELF/Members/Capability")]
public class MemberCapability : ScriptableObject
{
    [SerializeField] private string code;

    public string Code => code;
}