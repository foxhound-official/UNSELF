using System.Collections;
using TMPro;
using UnityEngine;

public class BootSequence : MonoBehaviour
{
    [SerializeField] private TMP_Text terminalText;
    [SerializeField] private WakeUpSequence wakeUpSequence;
    [SerializeField] private CompanyBootSequence companyBootSequence;
    [SerializeField] private MonitorBootTransition monitorBootTransition;

    [Header("Typing")]
    [SerializeField] private float characterDelay = 0.025f;
    [SerializeField] private float resultCharacterDelay = 0.04f;
    [SerializeField] private float resultPause = 0.2f;
    [SerializeField] private float linePause = 0.35f;

    [Header("Result Blink")]
    [SerializeField] private int blinkCount = 2;
    [SerializeField] private float blinkDelay = 0.08f;

    [Header("Final")]
    [SerializeField] private float finalDelay = 1.0f;
    [SerializeField] private float wakeDelay = 0.8f;

    private string completedText = "";

    private void Start()
    {
        StartCoroutine(PlayBootSequence());
    }

    private IEnumerator PlayBootSequence()
    {
        terminalText.text = "";
        completedText = "";

        yield return companyBootSequence.Play();

        yield return TypeSimpleLine(
            "> INITIALIZING MEMBER SYSTEM..."
        );

        yield return TypeStatusLine(
            "> HOST:",
            "DETECTED"
        );

        yield return TypeStatusLine(
            "> BIOLOGICAL STATUS:",
            "ACCEPTABLE"
        );

        yield return TypeStatusLine(
            "> MEMORY STRUCTURE:",
            "ERROR"
        );

        yield return TypeStatusLine(
            "> PERSONALITY CORE:",
            "NOT FOUND"
        );

        yield return TypeStatusLine(
            "> Y.O.U. MODULE:",
            "ACTIVE"
        );

        yield return TypeSimpleLine(
            "> ATTEMPTING RECOVERY..."
        );

        yield return TypeStatusLine(
            "> SYSTEM STATUS:",
            "READY"
        );

        yield return new WaitForSeconds(finalDelay);

        completedText += "\n";
        yield return TypeSimpleLine(
            "> WAKE",
            wakeDelay
        );

        wakeUpSequence.Begin();

        yield return monitorBootTransition.Play();

        gameObject.SetActive(false);
    }

    private IEnumerator TypeSimpleLine(
        string text,
        float pauseAfter = -1f)
    {
        string currentLine = "";

        foreach (char character in text)
        {
            currentLine += character;
            terminalText.text = completedText + currentLine;

            yield return new WaitForSeconds(characterDelay);
        }

        completedText += text + "\n";

        float pause =
            pauseAfter >= 0f
                ? pauseAfter
                : linePause;

        yield return new WaitForSeconds(pause);
    }

    private IEnumerator TypeStatusLine(
        string label,
        string result)
    {
        string currentLine = "";

        foreach (char character in label)
        {
            currentLine += character;
            terminalText.text = completedText + currentLine;

            yield return new WaitForSeconds(characterDelay);
        }

        yield return new WaitForSeconds(resultPause);

        string typedResult = "";

        foreach (char character in result)
        {
            typedResult += character;

            terminalText.text =
                completedText +
                label +
                " " +
                typedResult;

            yield return new WaitForSeconds(
                resultCharacterDelay
            );
        }

        for (int i = 0; i < blinkCount; i++)
        {
            // Result disappears
            terminalText.text =
                completedText +
                label +
                " ";

            yield return new WaitForSeconds(blinkDelay);

            // Result appears again
            terminalText.text =
                completedText +
                label +
                " " +
                result;

            yield return new WaitForSeconds(blinkDelay);
        }

        completedText +=
            label +
            " " +
            result +
            "\n";

        terminalText.text = completedText;

        yield return new WaitForSeconds(linePause);
    }
}