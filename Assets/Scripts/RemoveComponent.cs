/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public static class ExtensionMethods
{
    public static Component[] GetComponentsFunction(this GameObject go, System.Type type, bool includeChildren, bool includeInactive)
    {
        if (includeChildren)
        {
            return go.GetComponentsInChildren(type, includeInactive);
        }
        else
        {
            return go.GetComponents(type);
        }
    }
}

public class RemoveComponent : MonoBehaviour
{
    public GameObject[] targets;
    public bool includeChildren;
    public enum ComponentType
    {
        Collider,
        MeshCollider,
    }
    public ComponentType compType;

    public void RemoveComponents()
    {
        foreach (var target in targets)
        {
            Component[] components = new Component[0];
            switch (compType)
            {
                case ComponentType.Collider:
                    components = target.GetComponentsFunction(typeof(Collider), includeChildren, true);
                    break;
                case ComponentType.MeshCollider:
                    components = target.GetComponentsFunction(typeof(MeshCollider), includeChildren, true);
                    break;
            }

            foreach (var c in components)
            {
                DestroyImmediate(c);
            }
        }
    }
}