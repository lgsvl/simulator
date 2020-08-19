/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public abstract class TriggerEffector
{
    public abstract string TypeName { get; }

    public virtual AgentType[] UnsupportedAgentTypes { get; } = { AgentType.Unknown, AgentType.Ego};

    public abstract IEnumerator Apply(ITriggerAgent triggerAgent);

    public abstract void DeserializeProperties(JSONNode jsonData);
    
    public abstract void SerializeProperties(JSONNode jsonData);
}
