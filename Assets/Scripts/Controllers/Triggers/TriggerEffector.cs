/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;

public abstract class TriggerEffector
{
    public abstract string TypeName { get; }
    public float Value;

    public abstract IEnumerator Apply(NPCController parentNPC);
}
