/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "NewShaderInclusionBuildSettings", menuName = "Custom/ShaderInclusionBuildSettings", order = 1)]
public class ShaderInclusionBuildSettings : ScriptableObject
{
    public enum BundleTarget
    {
        Win64,
        Linux64,
    }

    [Tooltip("disable will use default project settings")]
    [SerializeField]
    private bool disable = false;
    [Header("Platform Specific Configuration")]
    [SerializeField]
    private List<Shader> Win64 = new List<Shader>();
    [SerializeField]
    private List<Shader> Linux64 = new List<Shader>();

    public List<Shader> GetShaderInclusionList(BundleTarget target)
    {
        if (disable)
        {
            return null;
        }

        switch (target)
        {
            case BundleTarget.Win64:
                return Win64;
            case BundleTarget.Linux64:
                return Linux64;
            default:
                return Win64;
        }
    }
}