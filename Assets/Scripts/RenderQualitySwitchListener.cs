/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public abstract class RenderQualitySwitchListener : MonoBehaviour
{
    protected virtual void Start()
    {
        var userInterface = FindObjectOfType<UserInterfaceSetup>();
        if (userInterface != null && userInterface.HighQualityRendering != null)
        {
            userInterface.HighQualityRendering.onValueChanged.AddListener(QualitySwitch);
        }
    }

    protected abstract void QualitySwitch(bool highQuality);
}
