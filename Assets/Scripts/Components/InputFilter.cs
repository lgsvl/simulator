/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Simulator
{
    public class Input : UnityEngine.Input
    {
        // Check if is currently focused on input field
        public static bool IsEditingInputField
        {
            get
            {
                var go = EventSystem.current?.currentSelectedGameObject;
                if (go != null)
                    return go.TryGetComponent(out InputField _);
                return false;
            }
        }

        public static new bool GetKeyDown(KeyCode key) => !IsEditingInputField && UnityEngine.Input.GetKeyDown(key);
        public static new bool GetKeyUp(KeyCode key) => !IsEditingInputField && UnityEngine.Input.GetKeyUp(key);
        public static new bool GetKey(KeyCode key) => !IsEditingInputField && UnityEngine.Input.GetKey(key);
    }
}
