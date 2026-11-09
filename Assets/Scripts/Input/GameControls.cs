
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GameControls
{
    private const string BindingsKey = "UNSELF_InputBindings";
    private const string SensitivityKey = "UNSELF_MouseSensitivity";
    private const string KeyboardMouseGroup = "Keyboard&Mouse";

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

    public static InputActionMap UIMap
    {
        get
        {
            Initialize();
            return InputSystem.actions.FindActionMap("UI", true);
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
        actions.FindActionMap("UI", true).Enable();

        initialized = true;
    }

    public static InputAction GetPlayerAction(string actionName)
    {
        return PlayerMap.FindAction(actionName, true);
    }

    public static InputAction GetUIAction(string actionName)
    {
        return UIMap.FindAction(actionName, true);
    }

    public static int FindBindingIndex(InputAction action, string bindingPart = null)
    {
        // Ищем основную привязку клавиатуры или мыши, а не конкретную клавишу.
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite)
                continue;

            if (!HasBindingGroup(binding.groups, KeyboardMouseGroup))
                continue;

            if (!string.IsNullOrEmpty(bindingPart) &&
                !string.Equals(binding.name, bindingPart, StringComparison.OrdinalIgnoreCase))
                continue;

            return i;
        }

        return -1;
    }


    public static string GetBindingLabel(string actionName)
    {
        InputAction action = GetPlayerAction(actionName);
        int index = FindBindingIndex(action);

        return GetBindingLabel(action, index);
    }

    public static string GetBindingLabel(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return "?";

        // Получаем название физической клавиши без учёта раскладки Windows.
        return action.bindings[bindingIndex].ToDisplayString();
    }

    private static bool HasBindingGroup(string groups, string groupName)
    {
        if (string.IsNullOrEmpty(groups))
            return false;

        foreach (string group in groups.Split(';'))
        {
            if (string.Equals(group, groupName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
