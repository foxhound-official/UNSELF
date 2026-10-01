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
    [SerializeField] private float wakeDelay = 0.25f;

    [Header("Audio")]
    [SerializeField] private int typingSoundInterval = 2;

    private string completedText = "";
    private int typedCharacterCount;

    private void Start()
    {
        StartCoroutine(PlayBootSequence());
    }

    private IEnumerator PlayBootSequence()
    {
        terminalText.text = "";
        completedText = "";
        typedCharacterCount = 0;

        AudioService.Play("interface_start");

        yield return companyBootSequence.Play();

        yield return TypeSimpleLine("> INITIALIZING MEMBER SYSTEM...");
        yield return TypeStatusLine("> HOST:", "[DETECTED]", "terminal_confirm");
        yield return TypeStatusLine("> BIOLOGICAL STATUS:", "[ACCEPTABLE]", "terminal_confirm");
        yield return TypeStatusLine("> MEMORY STRUCTURE:", "[ERROR]", "terminal_error");
        yield return TypeStatusLine("> PERSONALITY CORE:", "[NOT FOUND]", "terminal_error");
        yield return TypeStatusLine("> Y.O.U. MODULE:", "[ACTIVE]", "terminal_confirm");
        yield return TypeSimpleLine("> ATTEMPTING RECOVERY...");
        yield return TypeStatusLine("> SYSTEM STATUS:", "[READY]", "terminal_confirm");

        yield return new WaitForSeconds(finalDelay);

        completedText += "\n";
        yield return TypeSimpleLine("> WAKE", wakeDelay);

        wakeUpSequence.Begin();
        yield return monitorBootTransition.Play();

        gameObject.SetActive(false);
    }

    private IEnumerator TypeSimpleLine(string text, float pauseAfter = -1f)
    {
        string currentLine = "";

        foreach (char character in text)
        {
            currentLine += character;
            terminalText.text = completedText + currentLine;
            PlayTypingSound(character);

            yield return new WaitForSeconds(characterDelay);
        }

        completedText += text + "\n";

        float pause = pauseAfter >= 0f ? pauseAfter : linePause;
        yield return new WaitForSeconds(pause);
    }

    private IEnumerator TypeStatusLine(string label, string result, string resultSoundId)
    {
        string currentLine = "";

        foreach (char character in label)
        {
            currentLine += character;
            terminalText.text = completedText + currentLine;
            PlayTypingSound(character);

            yield return new WaitForSeconds(characterDelay);
        }

        yield return new WaitForSeconds(resultPause);

        string typedResult = "";

        foreach (char character in result)
        {
            typedResult += character;
            terminalText.text = completedText + label + " " + typedResult;
            PlayTypingSound(character);

            yield return new WaitForSeconds(resultCharacterDelay);
        }

        AudioService.Play(resultSoundId);

        for (int i = 0; i < blinkCount; i++)
        {
            terminalText.text = completedText + label + " ";
            yield return new WaitForSeconds(blinkDelay);

            terminalText.text = completedText + label + " " + result;
            yield return new WaitForSeconds(blinkDelay);
        }

        completedText += label + " " + result + "\n";
        terminalText.text = completedText;

        yield return new WaitForSeconds(linePause);
    }

    private void PlayTypingSound(char character)
    {
        if (char.IsWhiteSpace(character))
            return;

        typedCharacterCount++;

        if (typedCharacterCount % typingSoundInterval != 0)
            return;

        AudioService.Play("terminal_key");
    }
}