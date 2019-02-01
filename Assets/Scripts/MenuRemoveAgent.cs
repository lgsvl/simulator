/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuRemoveAgent : MonoBehaviour
{
    public GameObject rootGO;

    public void RemoveAgent()
    {
        ROSAgentManager.Instance?.Remove(rootGO);
        Destroy(rootGO);
    }
}
