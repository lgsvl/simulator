/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Linq;

public class JointSelfCollisionDisabler : MonoBehaviour
{
    public Transform jointParent = null;

    void Awake()
    {
        var childColliders = transform.GetComponentsInChildren<Collider>().Where(c => c.transform.parent == transform).ToArray();
        var parentColliders = jointParent.GetComponentsInChildren<Collider>().Where(c => c.transform.parent == jointParent).ToArray();

        if (childColliders.Length == 0)
        {
            Debug.Log("no child colliders found!" + transform);
        }

        if (parentColliders.Length == 0)
        {
            Debug.Log("no parent colliders found!" + jointParent);
        }

        foreach (var cc in childColliders)
        {
            foreach (var pc in parentColliders)
            {
                Physics.IgnoreCollision(pc, cc);
            }
        }
    }
}

