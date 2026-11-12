using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UiClickSound : MonoBehaviour
{
    [SerializeField] private string soundId = "ui_click";

    private readonly List<RaycastResult> raycastResults = new();

    private InputAction clickAction;
    private InputAction pointAction;

    private void Awake()
    {
        clickAction = GameControls.GetUIAction("Click");
        pointAction = GameControls.GetUIAction("Point");
    }

    private void Update()
    {
        if (!clickAction.WasPressedThisFrame() || EventSystem.current == null)
            return;

        PointerEventData pointerData = new(EventSystem.current)
        {
            position = pointAction.ReadValue<Vector2>()
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            Button button = result.gameObject.GetComponentInParent<Button>();

            if (button == null || !button.interactable)
                continue;

            AudioService.PlayUI(soundId);
            return;
        }
    }
}