/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEngine;

public abstract class AgentController : MonoBehaviour
{
    public abstract void SetWheelScale(float value);
    public abstract void ResetPosition();
    public abstract void ResetSavedPosition(Vector3 pos, Quaternion rot);
}
