
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GameControls
{
    private const string BindingsKey = "UNSELF_InputBindings";
    private const string SensitivityKey = "UNSELF_MouseSensitivity";

    private static bool initialized;

    public static event Action BindingsChanged;

    public static InputActionMap PlayerMap
    {
        get
        {
            Initialize();
            return InputSystem.actions.FindActionMap("Player", true);
        }
    }

    public static float MouseSensitivityMultiplier
    {
        get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.5f, 2f));
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        initialized = false;
        Initialize();
    }

    public static void Initialize()
    {
        if (initialized)
            return;

        InputActionAsset actions = InputSystem.actions;

        if (actions == null)
        {
            Debug.LogError("Project-wide Input Actions are not configured.");
            return;
        }

        string savedBindings = PlayerPrefs.GetString(BindingsKey, "");

        if (!string.IsNullOrEmpty(savedBindings))
        {
            try
            {
                actions.LoadBindingOverridesFromJson(savedBindings);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load input bindings: {exception.Message}");
            }
        }

        actions.FindActionMap("Player", true).Enable();
        initialized = true;
    }

    public static InputAction GetPlayerAction(string actionName)
    {
        return PlayerMap.FindAction(actionName, true);
    }

    public static int FindBindingIndex(InputAction action, string originalPath)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (string.Equals(action.bindings[i].path, originalPath, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public static string GetBindingLabel(string actionName, string originalPath)
    {
        InputAction action = GetPlayerAction(actionName);
        int index = FindBindingIndex(action, originalPath);

        return index >= 0 ? action.GetBindingDisplayString(index) : "?";
    }

    public static bool HasBindingConflict(InputAction editedAction, int editedIndex)
    {
        string newPath = editedAction.bindings[editedIndex].effectivePath;

        foreach (InputAction action in PlayerMap.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action == editedAction && i == editedIndex)
                    continue;

                InputBinding binding = action.bindings[i];

                if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath))
                    continue;

                if (string.Equals(binding.effectivePath, newPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public static void SaveBindings()
    {
        PlayerPrefs.SetString(BindingsKey, InputSystem.actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        BindingsChanged?.Invoke();
    }

    public static void ResetBindings()
    {
        InputSystem.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingsKey);
        PlayerPrefs.Save();
        BindingsChanged?.Invoke();
    }
}