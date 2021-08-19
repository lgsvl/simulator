/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using UnityEngine;

public interface ITriggerAgent
{
    /// <summary>
    /// Agent transform that is moving
    /// </summary>
    Transform AgentTransform { get; }
    
    /// <summary>
    /// Speed which agent has while moving
    /// </summary>
    float MovementSpeed { get; }
    
    /// <summary>
    /// Acceleration which agent currently have
    /// </summary>
    Vector3 Acceleration { get; }

    /// <summary>
    /// Starts a coroutine
    /// </summary>
    /// <param name="enumerator">Coroutine enumerator that will be execute</param>
    /// <returns>Started Coroutine</returns>
    Coroutine StartCoroutine(IEnumerator enumerator);
}
