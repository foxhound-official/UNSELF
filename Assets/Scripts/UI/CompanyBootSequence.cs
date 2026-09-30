using System.Collections;
using TMPro;
using UnityEngine;

public class CompanyBootSequence : MonoBehaviour
{
    [SerializeField] private TMP_Text aurelisLogo;
    [SerializeField] private TMP_Text aurelisText;

    [SerializeField] private float lineDelay = 0.1f;
    [SerializeField] private float subtitleCharacterDelay = 0.025f;
    [SerializeField] private float pauseAfterLogo = 0.15f;

    public IEnumerator Play()
    {
        aurelisLogo.ForceMeshUpdate();

        aurelisLogo.maxVisibleCharacters = 0;
        aurelisText.maxVisibleCharacters = 0;

        TMP_TextInfo textInfo = aurelisLogo.textInfo;

        for (int lineIndex = 0; lineIndex < textInfo.lineCount; lineIndex++)
        {
            TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];

            aurelisLogo.maxVisibleCharacters =
                lineInfo.lastCharacterIndex + 1;

            yield return new WaitForSeconds(lineDelay);
        }

        yield return new WaitForSeconds(pauseAfterLogo);

        for (int i = 0; i <= aurelisText.text.Length; i++)
        {
            aurelisText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(subtitleCharacterDelay);
        }
    }
}