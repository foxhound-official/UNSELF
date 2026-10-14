using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private Light flashLight;
    [SerializeField] private GameObject flashVisual;
    [SerializeField] private float flashDuration = 0.04f;

    private float flashEndTime;
    private bool isActive;

    private void Awake()
    {
        SetFlashActive(false);
    }

    private void Update()
    {
        if (!isActive || Time.time < flashEndTime)
            return;

        SetFlashActive(false);
    }

    public void Play()
    {
        flashEndTime = Time.time + flashDuration;
        SetFlashActive(true);
    }

    private void SetFlashActive(bool active)
    {
        isActive = active;

        if (flashLight != null)
            flashLight.enabled = active;

        if (flashVisual != null)
            flashVisual.SetActive(active);
    }
}