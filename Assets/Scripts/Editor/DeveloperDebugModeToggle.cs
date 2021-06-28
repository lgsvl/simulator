/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Web;
using UnityEditor;

[InitializeOnLoad]
public static class DeveloperDebugModeToggle
{
    private const string MENU_NAME = "Simulator/Developer Debug Mode";

    private static bool enabled_;
    /// Called on load thanks to the InitializeOnLoad attribute
    static DeveloperDebugModeToggle()
    {
        DeveloperDebugModeToggle.enabled_ = EditorPrefs.GetBool(DeveloperDebugModeToggle.MENU_NAME, false);
        //DeveloperDebugModeToggle.enabled_ = Config.DeveloperDebugModeEnabled; // TODO why this not working?
        /// Set checkmark on menu item
        Menu.SetChecked(DeveloperDebugModeToggle.MENU_NAME, Config.DeveloperDebugModeEnabled);

        /// Delaying until first editor tick so that the menu
        /// will be populated before setting check state, and
        /// re-apply correct action
        EditorApplication.delayCall += () => {
            PerformAction(DeveloperDebugModeToggle.enabled_);
        };
    }

    [MenuItem(DeveloperDebugModeToggle.MENU_NAME)]
    private static void ToggleAction()
    {
        /// Toggling action
        PerformAction(!DeveloperDebugModeToggle.enabled_);
    }

    public static void PerformAction(bool enabled)
    {
        /// Set checkmark on menu item
        Menu.SetChecked(DeveloperDebugModeToggle.MENU_NAME, enabled);
        /// Saving editor state
        EditorPrefs.SetBool(DeveloperDebugModeToggle.MENU_NAME, enabled);
        //Config.DeveloperDebugModeEnabled = enabled; // TODO why this not working?

        DeveloperDebugModeToggle.enabled_ = enabled;
    }
}
