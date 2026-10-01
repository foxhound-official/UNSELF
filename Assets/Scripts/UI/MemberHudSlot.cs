using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MemberHudSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text[] logLines;

    [SerializeField] private Color emptyColor = new Color(0.55f, 0.55f, 0.55f);

    [Header("Log")]
    [SerializeField] private float logLifetime = 4f;
    [SerializeField] private float logFadeDuration = 1f;

    private readonly List<LogEntry> entries = new();
    private Color memberColor = Color.white;

    private class LogEntry
    {
        public string message;
        public float age;
    }

    private void Awake()
    {
        SetEmpty();
        RefreshLog();
    }

    private void Update()
    {
        if (entries.Count == 0)
            return;

        foreach (LogEntry entry in entries)
            entry.age += Time.deltaTime;

        LogEntry oldest = entries[0];

        if (oldest.age >= logLifetime + logFadeDuration)
        {
            entries.RemoveAt(0);
            RefreshLog();
            return;
        }

        RefreshLog();
    }

    public void SetEmpty()
    {
        headerText.text = "[EMPTY]";
        statusText.text = "> NO ACTIVE MEMBER";

        memberColor = emptyColor;

        headerText.color = memberColor;
        statusText.color = memberColor;

        entries.Clear();
        RefreshLog();
    }

    public void SetMember(MemberDefinition member)
    {
        memberColor = GetClassColor(member.Class);

        headerText.text = $"{member.Code} // {member.MemberName}";
        statusText.text = $"> {member.Class.ToString().ToUpper()} MEMBER\n> ACTIVE";

        headerText.color = memberColor;
        statusText.color = memberColor;

        RefreshLog();
    }

    public void ShowLog(string message)
    {
        if (entries.Count >= logLines.Length)
            entries.RemoveAt(0);

        entries.Add(new LogEntry
        {
            message = message,
            age = 0f
        });

        AudioService.Play("member_log");

        RefreshLog();
    }

    private void RefreshLog()
    {
        for (int i = 0; i < logLines.Length; i++)
        {
            if (i >= entries.Count)
            {
                logLines[i].text = "";
                continue;
            }

            LogEntry entry = entries[i];

            logLines[i].text = entry.message;

            Color color = memberColor;

            if (i == 0 && entry.age > logLifetime)
            {
                float fadeProgress = (entry.age - logLifetime) / logFadeDuration;
                color.a = 1f - Mathf.Clamp01(fadeProgress);
            }
            else
            {
                color.a = 1f;
            }

            logLines[i].color = color;
        }
    }

    private Color GetClassColor(MemberClass memberClass)
    {
        return memberClass switch
        {
            MemberClass.Social => new Color(0.30f, 0.70f, 1.00f),
            MemberClass.Technical => new Color(1.00f, 0.75f, 0.20f),
            MemberClass.Combat => new Color(1.00f, 0.30f, 0.25f),
            _ => Color.white
        };
    }
}