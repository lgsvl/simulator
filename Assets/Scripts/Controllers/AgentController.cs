/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    public abstract void ResetPosition();
    public abstract void ResetSavedPosition(Vector3 pos, Quaternion rot);
    public abstract void Init();
    public bool Active { get; set; }
}
