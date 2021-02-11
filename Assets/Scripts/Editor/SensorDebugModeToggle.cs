using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class SensorDebugModeToggle
{
    private const string MENU_NAME = "Simulator/Sensor Debug Mode";

    private static bool enabled_;
    /// Called on load thanks to the InitializeOnLoad attribute
    static SensorDebugModeToggle()
    {
        SensorDebugModeToggle.enabled_ = EditorPrefs.GetBool(SensorDebugModeToggle.MENU_NAME, false);

        /// Delaying until first editor tick so that the menu
        /// will be populated before setting check state, and
        /// re-apply correct action
        EditorApplication.delayCall += () => {
            PerformAction(SensorDebugModeToggle.enabled_);
        };
    }

    [MenuItem(SensorDebugModeToggle.MENU_NAME)]
    private static void ToggleAction()
    {

        /// Toggling action
        PerformAction(!SensorDebugModeToggle.enabled_);
    }

    public static void PerformAction(bool enabled)
    {
        /// Set checkmark on menu item
        Menu.SetChecked(SensorDebugModeToggle.MENU_NAME, enabled);
        /// Saving editor state
        EditorPrefs.SetBool(SensorDebugModeToggle.MENU_NAME, enabled);

        SensorDebugModeToggle.enabled_ = enabled;
    }
}
