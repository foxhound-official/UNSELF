using System.Collections;
using TMPro;
using UnityEngine;

public class BootSequence : MonoBehaviour
{
    [SerializeField] private TMP_Text terminalText;
    [SerializeField] private WakeUpSequence wakeUpSequence;
    [SerializeField] private float lineDelay = 0.6f;
    [SerializeField] private float finalDelay = 1.5f;

    private readonly string[] bootLines =
    {
        "> INITIALIZING MEMBER SYSTEM...",
        "> HOST DETECTED",
        "> BIOLOGICAL STATUS: ACCEPTABLE",
        "> MEMORY STRUCTURE: ERROR",
        "> PERSONALITY CORE: NOT FOUND",
        "> Y.O.U. MODULE: ACTIVE",
        "> ATTEMPTING RECOVERY...",
        "> SYSTEM READY",
        "> WAKE"
    };

    private void Start()
    {
        StartCoroutine(PlayBootSequence());
    }

    private IEnumerator PlayBootSequence()
    {
        terminalText.text = "";

        foreach (string line in bootLines)
        {
            terminalText.text += line + "\n";
            yield return new WaitForSeconds(lineDelay);
        }

        yield return new WaitForSeconds(finalDelay);

        wakeUpSequence.Begin();
        gameObject.SetActive(false);
    }
}