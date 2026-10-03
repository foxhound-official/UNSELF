using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UiClickSound : MonoBehaviour
{
    [SerializeField] private string soundId = "ui_click";

    private readonly List<RaycastResult> raycastResults = new();

    private void Update()
    {
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame ||
            EventSystem.current == null)
        {
            return;
        }

        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            Button button = result.gameObject.GetComponentInParent<Button>();

            if (button == null || !button.interactable)
                continue;

            AudioService.Play(soundId);
            return;
        }
    }
}