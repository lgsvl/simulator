/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class CreateComponent : MonoBehaviour
{
    public GameObject[] targets;
    public enum ConditionComponentType
    {
        Any,
        MeshFilter,
        MeshRenderer,
    }
    public ConditionComponentType conditionType;

    public enum ComponentType
    {
        Collider,
        MeshCollider,
    }
    public ComponentType compType;

    public bool ignoreExisting = true;

    public void CreateComponents()
    {
        foreach (var target in targets)
        {
            Component[] components = new Component[0];
            switch (conditionType)
            {
                case ConditionComponentType.Any:
                    components = target.GetComponentsFunction(typeof(Component), true, true);
                    break;
                case ConditionComponentType.MeshFilter:
                    components = target.GetComponentsFunction(typeof(MeshFilter), true, true);
                    break;
                case ConditionComponentType.MeshRenderer:
                    components = target.GetComponentsFunction(typeof(MeshRenderer), true, true);
                    break;
            }

            foreach (var c in components)
            {
                System.Type type = null;
                switch (compType)
                {
                    case ComponentType.Collider:
                        type = typeof(Collider);
                        break;
                    case ComponentType.MeshCollider:
                        type = typeof(MeshCollider);
                        break;
                }
                if (ignoreExisting || c.GetComponent(type) == null)
                {
                    c.gameObject.AddComponent(type);
                }
            }
        }
    }
}