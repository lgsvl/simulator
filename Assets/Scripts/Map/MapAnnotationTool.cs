/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapAnnotationTool : MonoBehaviour
{
    public static bool SHOW_MAP_ALL { get; private set; } = false;
    public static bool SHOW_MAP_SELECTED { get; private set; } = true;
    public static float PROXIMITY { get; private set; } = 1.0f;
    public static float ARROWSIZE { get; private set; } = 50f;
}
