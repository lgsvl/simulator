/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using SimpleJSON;

public abstract class TriggerEffector : ICloneable
{
    public abstract string TypeName { get; }

    public virtual AgentType[] UnsupportedAgentTypes { get; } = { AgentType.Unknown, AgentType.Ego};
    
    public abstract object Clone();

    public abstract IEnumerator Apply(ITriggerAgent triggerAgent);
    
    public abstract void SerializeProperties(JSONNode jsonData);

    public abstract void DeserializeProperties(JSONNode jsonData);
}
