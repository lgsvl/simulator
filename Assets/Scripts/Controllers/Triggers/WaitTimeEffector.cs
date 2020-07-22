/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using SimpleJSON;
using UnityEngine;

public class WaitTimeEffector : TriggerEffector
{
    public override string TypeName { get; } = "WaitTime";
    public float Value;

    public override IEnumerator Apply(NPCController parentNPC)
    {
        //Make parent npc wait for "value" time
        yield return new WaitForSeconds(Value);
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        Value = jsonData["value"];
    }

    public override void SerializeProperties(JSONNode jsonData)
    {
        jsonData.Add("value", new JSONNumber(Value));
    }
}
