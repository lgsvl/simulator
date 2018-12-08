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
        Animator
        //Custom
    };

    [Tooltip("Select component type")]
    public ComponentType componentType;

    //[Tooltip("Custom component type")]
    //public string customComponent;

    [Tooltip("Remove child components")]
    public bool isRemoveChildren = false;

    [MenuItem("SimulatorUtil/Remove Component Tool")]
    static void CreateWizard()
    {
        DisplayWizard("Remove Component Tool", typeof(RemoveComponentTool), "Remove");
    }

    void OnWizardCreate()
    {
        int count = 0;
        foreach (Transform t in Selection.transforms)
        {
            
            if (isRemoveChildren)
            {
                Component[] components = new Component[0];
                
                System.Type type = System.Type.GetType("UnityEngine." + componentType.ToString() + ", UnityEngine", true);
                if (type == null) return;
                components = t.GetComponentsInChildren(type, true);

                foreach (var component in components)
                {
                    count++;
                    Undo.DestroyObjectImmediate(component);
                }
            }
            else
            {
                count++;
                Undo.DestroyObjectImmediate(t.GetComponent(componentType.ToString()));
                
                //if (componentType != ComponentType.Custom)
                //    Undo.DestroyObjectImmediate(t.GetComponent(componentType.ToString()));
                //else
                //    Undo.DestroyObjectImmediate(t.GetComponent(customComponent));
            }
        }
        Debug.Log($"Finished Removing {count} {componentType.ToString()}(s)");
    }
}
