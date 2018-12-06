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
                //Undo.DestroyObjectImmediate(t.GetComponentsInChildren(System.Type.GetType(componentType.ToString()), true)); // NEEDS OBJECT[]

                //Undo.DestroyObjectImmediate(t.GetComponentsInChildren(System.Type.GetType(customComponent), true));
            }
            else
            {
                Undo.DestroyObjectImmediate(t.GetComponent(componentType.ToString()));

                Undo.DestroyObjectImmediate(t.GetComponent(customComponent));
            }

            if (componentType != ComponentType.Custom)
            {
                Undo.DestroyObjectImmediate(t.GetComponent(componentType.ToString()));
            }
            else
            {
                Undo.DestroyObjectImmediate(t.GetComponent(customComponent));
            }
        }

        //foreach (GameObject go in Selection.gameObjects)
        //{
        //    Undo.DestroyObjectImmediate(go);
        //}
    }

}
