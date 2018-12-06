/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RemoveComponentTool : ScriptableWizard
{
    public enum ComponentType
    {
        Collider,
        BoxCollider,
        MeshCollider,
        CapsuleCollider,
        SphereCollider,
        WheelCollider,
        Custom
    };

    [Tooltip("Select component type")]
    public ComponentType componentType;

    [Tooltip("Custom component type")]
    public string customComponent;

    [Tooltip("Remove child components")]
    public bool isRemoveChildren = false;

    [MenuItem("SimulatorUtil/Remove Component Tool")]
    static void CreateWizard()
    {
        DisplayWizard("Remove Component Tool", typeof(RemoveComponentTool), "Remove");
    }

    void OnWizardCreate()
    {
        foreach (Transform t in Selection.transforms)
        {
            if (isRemoveChildren)
            {
                Component[] components = new Component[0];

                if (componentType != ComponentType.Custom)
                    components = t.GetComponentsInChildren(System.Type.GetType(componentType.ToString()), true);
                else
                    components = t.GetComponentsInChildren(System.Type.GetType(customComponent), true);

                foreach (var component in components)
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }
            else
            {
                if (componentType != ComponentType.Custom)
                    Undo.DestroyObjectImmediate(t.GetComponent(componentType.ToString()));
                else
                    Undo.DestroyObjectImmediate(t.GetComponent(customComponent));
            }

            
        }
    }
}
