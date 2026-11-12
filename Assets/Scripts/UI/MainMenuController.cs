using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject settingsPage;

    [Header("Music")]
    [SerializeField] private string menuMusicId = "main_menu";

    [Header("Mouse Settings")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;

    private void Start()
    {
        GameControls.Initialize();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        mainPage.SetActive(true);
        settingsPage.SetActive(false);

        sensitivitySlider.minValue = 0.5f;
        sensitivitySlider.maxValue = 2f;
        sensitivitySlider.SetValueWithoutNotify(GameControls.MouseSensitivityMultiplier);

        UpdateSensitivityLabel();

        if (MusicService.Instance != null)
            MusicService.Instance.Play(menuMusicId);
    }

    public void StartGame()
    {
        if (MusicService.Instance != null)
            MusicService.Instance.Stop();

        SceneManager.LoadSceneAsync("Act01_Scrapyard");
    }

    public void OpenSettings()
    {
        mainPage.SetActive(false);
        settingsPage.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPage.SetActive(false);
        mainPage.SetActive(true);
    }

    public void SetMouseSensitivity(float value)
    {
        GameControls.MouseSensitivityMultiplier = value;
        UpdateSensitivityLabel();
    }

    public void ResetControls()
    {
        GameControls.ResetBindings();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateSensitivityLabel()
    {
        if (sensitivityValueText == null)
            return;

        sensitivityValueText.text = $"{Mathf.RoundToInt(GameControls.MouseSensitivityMultiplier * 100f)}%";
    }
}