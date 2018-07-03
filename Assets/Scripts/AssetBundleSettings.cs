/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAssetBundleSettings", menuName = "Custom/AssetBundleSettings", order = 1)]
public class AssetBundleSettings : ScriptableObject
{
    public List<MapInfo> maps = new List<MapInfo>();
}