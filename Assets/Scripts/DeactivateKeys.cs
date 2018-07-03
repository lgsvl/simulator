/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeactivateKeys : MonoBehaviour
{
    void Start()
    {
        var input = GetComponent<InputField>();
        if (input != null)
        {
            input.onEndEdit.AddListener(value =>
            {
                EventSystem.current.SetSelectedGameObject(null);
            });
        }
    }
}
