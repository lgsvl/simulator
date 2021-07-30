/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Map;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class NPCMetaData : MonoBehaviour
{
    public NPCSizeType SizeType = NPCSizeType.MidSize;
    public Rigidbody RefRB;
    public GameObject WheelColliderHolder;
    public List<NPCController.WheelData> WheelData = new List<NPCController.WheelData>(); // TODO align with AxleInfo
}
