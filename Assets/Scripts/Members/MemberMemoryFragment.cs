using UnityEngine;

[CreateAssetMenu(
    fileName = "MemoryFragment",
    menuName = "UNSELF/Members/Memory Fragment"
)]
public class MemberMemoryFragment : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private string title;

    [Header("Memory")]
    [SerializeField, TextArea(5, 12)]
    private string content;

    [Header("Context")]
    [SerializeField, TextArea(3, 7)]
    private string contextNotes;

    public string Id => id;
    public string Title => title;
    public string Content => content;
    public string ContextNotes => contextNotes;
}