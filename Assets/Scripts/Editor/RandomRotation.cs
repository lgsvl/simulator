/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEditor;
using UnityEngine;

public class MenuTest : MonoBehaviour
{
    [MenuItem("SimulatorUtil/Random Y Rotation")]

    static void RandomizeSelectionYRotation()
    {
        foreach (var t in Selection.transforms)
        {
            t.Rotate(0, Random.Range(0, 360f), 0);
        }
    }
}