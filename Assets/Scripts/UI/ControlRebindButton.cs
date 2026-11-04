using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlRebindButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text bindingText;

    [Header("Input Binding")]
    [SerializeField] private string actionName;
    [SerializeField] private string bindingPart;

    private static ControlRebindButton activeRebind;

    private InputAction action;
    private int bindingIndex = -1;
    private string previousOverride;

    private InputActionRebindingExtensions.RebindingOperation operation;

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        action = GameControls.GetPlayerAction(actionName);
        bindingIndex = GameControls.FindBindingIndex(action, bindingPart);

        if (bindingIndex < 0)
        {
            Debug.LogError($"Binding not found: {actionName} / {bindingPart}", this);
            return;
        }

        button.onClick.AddListener(StartRebind);
        GameControls.BindingsChanged += RefreshLabel;

        RefreshLabel();
    }

    private void OnDisable()
    {
        if (operation != null)
            operation.Cancel();

        if (button != null)
            button.onClick.RemoveListener(StartRebind);

        GameControls.BindingsChanged -= RefreshLabel;
    }

    public void RefreshLabel()
    {
        if (action == null || bindingIndex < 0 || bindingText == null)
            return;

        bindingText.text = action.GetBindingDisplayString(bindingIndex);
    }

    private void StartRebind()
    {
        if (activeRebind != null || action == null || bindingIndex < 0)
            return;

        activeRebind = this;
        previousOverride = action.bindings[bindingIndex].overridePath;

        action.Disable();

        bindingText.text = "PRESS KEY... ESC TO CANCEL";

        operation = action.PerformInteractiveRebinding(bindingIndex)
            .WithExpectedControlType("Button")
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Gamepad>/*")
            .WithControlsExcluding("<Joystick>/*")
            .WithControlsExcluding("<Touchscreen>/*")
            .WithControlsExcluding("<XRController>/*")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(_ => FinishRebind(true))
            .OnCancel(_ => FinishRebind(false));

        // Для клавиатурных действий исключаем случайный выбор кнопок мыши.
        string originalPath = action.bindings[bindingIndex].path;

        if (originalPath.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase))
            operation.WithControlsExcluding("<Mouse>/*");

        operation.Start();
    }

    private void FinishRebind(bool completed)
    {
        bool hasConflict = completed && GameControls.HasBindingConflict(action, bindingIndex);

        if (!completed || hasConflict)
        {
            if (string.IsNullOrEmpty(previousOverride))
                action.RemoveBindingOverride(bindingIndex);
            else
                action.ApplyBindingOverride(bindingIndex, previousOverride);

            if (hasConflict)
                Debug.LogWarning("This control is already assigned to another action.");
        }

        operation.Dispose();
        operation = null;

        action.Enable();
        activeRebind = null;

        if (completed && !hasConflict)
            GameControls.SaveBindings();

        RefreshLabel();
    }
}