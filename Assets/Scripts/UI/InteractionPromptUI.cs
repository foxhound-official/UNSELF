using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        promptText.text = text;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}