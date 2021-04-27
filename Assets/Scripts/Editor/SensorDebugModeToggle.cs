/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Web;
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
        //SensorDebugModeToggle.enabled_ = Config.SensorDebugModeEnabled; // TODO why this not working?
        /// Set checkmark on menu item
        Menu.SetChecked(SensorDebugModeToggle.MENU_NAME, Config.SensorDebugModeEnabled);

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
        //Config.SensorDebugModeEnabled = enabled; // TODO why this not working?

        SensorDebugModeToggle.enabled_ = enabled;
    }
}
