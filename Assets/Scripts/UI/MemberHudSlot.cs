
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MemberHudSlot : MonoBehaviour
{
    [Header("Member Info")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text statusText;

    [Header("Log Lines")]
    [SerializeField] private TMP_Text[] logLines;
    [SerializeField] private bool stackLogsBottomUp;

    [SerializeField] private Color emptyColor =
        new Color(0.75f, 0.75f, 0.75f);

    [Header("Log Timing")]
    [SerializeField, Min(0f)] private float logLifetime = 4f;
    [SerializeField, Min(0f)] private float logFadeDuration = 1f;
    [SerializeField, Min(0f)] private float logAppearDuration = 0.15f;

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
        memberColor = MemberClassColors.Get(member.Class);

        headerText.text =
            $"{member.Code} // {member.MemberName}";

        statusText.text =
            $"> {member.Class.ToString().ToUpper()} MEMBER\n> ACTIVE";

        headerText.color = memberColor;
        statusText.color = memberColor;

        RefreshLog();
    }

    public void ShowLog(string message)
    {
        if (logLines == null || logLines.Length == 0)
            return;

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
        if (logLines == null || logLines.Length == 0)
            return;

        // Сначала очищаем все визуальные строки.
        foreach (TMP_Text line in logLines)
        {
            if (line != null)
                line.text = string.Empty;
        }

        int visibleCount = Mathf.Min(
            entries.Count,
            logLines.Length
        );

        // Для нижнего левого HUD заполняем строки
        // начиная с нижней, а не с верхней.
        int startLine = stackLogsBottomUp
            ? logLines.Length - visibleCount
            : 0;

        for (int i = 0; i < visibleCount; i++)
        {
            int lineIndex = startLine + i;
            int entryIndex = entries.Count - visibleCount + i;

            TMP_Text line = logLines[lineIndex];

            if (line == null)
                continue;

            LogEntry entry = entries[entryIndex];

            line.text = entry.message;

            // Плавное появление нового сообщения.
            float appearAlpha = logAppearDuration > 0f
                ? Mathf.Clamp01(entry.age / logAppearDuration)
                : 1f;

            float fadeAlpha = 1f;

            // Старейшее сообщение постепенно исчезает.
            if (entryIndex == 0 && entry.age > logLifetime)
            {
                if (logFadeDuration > 0f)
                {
                    float fadeProgress =
                        (entry.age - logLifetime) /
                        logFadeDuration;

                    fadeAlpha = 1f -
                        Mathf.Clamp01(fadeProgress);
                }
                else
                {
                    fadeAlpha = 0f;
                }
            }

            Color color = memberColor;
            color.a *= appearAlpha * fadeAlpha;

            line.color = color;
        }
    }

    private Color GetClassColor(MemberClass memberClass)
    {
        return memberClass switch
        {
            MemberClass.Social =>
                new Color(0.30f, 0.70f, 1.00f),

            MemberClass.Technical =>
                new Color(1.00f, 0.75f, 0.20f),

            MemberClass.Combat =>
                new Color(1.00f, 0.30f, 0.25f),

            MemberClass.Agility =>
                new Color(0.28f, 0.84f, 0.66f),

            _ => Color.white
        };
    }
}
