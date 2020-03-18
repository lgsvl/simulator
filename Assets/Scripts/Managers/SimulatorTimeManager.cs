/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Net;
using Simulator;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Core.Threading;
using UnityEngine;

/// <summary>
/// Simulator time manager allows to control settings like time scale in whole simulation
/// </summary>
public class SimulatorTimeManager : IMessageReceiver, IMessageSender
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    private static SimulatorTimeManager instance; 
    
    /// <summary>
    /// Messages manager handling the distributed messages
    /// </summary>
    private MessagesManager messagesManager;
    
    /// <summary>
    /// Time scale which is applied when time scale is unlocked
    /// </summary>
    private float timeScale = 1.0f;

    /// <inheritdoc/>
    public string Key { get; } = "SimulatorTimeManager";

    /// <summary>
    /// Semaphore for locking the simulation time scale, if locked timescale 0.0f is applied
    /// </summary>
    public LockingSemaphore TimeScaleSemaphore { get; } = new LockingSemaphore();

    /// <summary>
    /// Time scale which is applied when time scale is unlocked
    /// </summary>
    public float TimeScale
    {
        get => TimeScaleSemaphore.IsUnlocked ? timeScale : 0.0f;
        set
        {
            timeScale = value;
            if (TimeScaleSemaphore.IsUnlocked) 
                SetUnityTimeScale(timeScale);
        }
    }

    /// <summary>
    /// Initialization method required to distribute the timescale
    /// </summary>
    /// <param name="messagesManager">Messages manager handling the distributed messages</param>
    protected internal void Initialize(MessagesManager messagesManager)
    {
        TimeScaleSemaphore.Locked += TimeScaleSemaphoreOnLocked;
        TimeScaleSemaphore.Unlocked += TimeScaleSemaphoreOnUnlocked;
        this.messagesManager = messagesManager;
        messagesManager?.RegisterObject(this);
        Debug.Assert(instance==null);
        instance = this;
    }

    /// <summary>
    /// Deinitialization method
    /// </summary>
    protected internal void Deinitialize()
    {
        TimeScaleSemaphore.Locked -= TimeScaleSemaphoreOnLocked;
        TimeScaleSemaphore.Unlocked -= TimeScaleSemaphoreOnUnlocked;
        messagesManager?.UnregisterObject(this);
        messagesManager = null;
        instance = null;
    }

    /// <summary>
    /// Method called when the time scale semaphore becomes unlocked
    /// </summary>
    private void TimeScaleSemaphoreOnUnlocked()
    {
        SetUnityTimeScale(timeScale);
    }

    /// <summary>
    /// Method called when the time scale semaphore becomes locked
    /// </summary>
    private void TimeScaleSemaphoreOnLocked()
    {
        SetUnityTimeScale(0.0f);
    }

    /// <summary>
    /// Sets the Unity time scale, omits the locks and set time scale of all time manager instances
    /// </summary>
    /// <param name="unityTimeScale">Time scale which will be applied</param>
    public static void SetUnityTimeScale(float unityTimeScale)
    {
        Time.timeScale = unityTimeScale;
        if (Mathf.Approximately(unityTimeScale, 0.0f))
        {
            Physics.autoSimulation = false;
            Time.fixedDeltaTime = 0.01f;
        }
        else
        {
            Physics.autoSimulation = true;
            Time.fixedDeltaTime = 0.01f / unityTimeScale;
        }

        //Try to distribute this time scale if there is time manager instance set in the simulator manager
        var network = Loader.Instance.Network;
        if (instance == null || !network.IsMaster) return;
        var message = MessagesPool.Instance.GetMessage(4);
        message.Content.PushFloat(Time.timeScale);
        message.AddressKey = instance.Key;
        message.Type = DistributedMessageType.ReliableOrdered;
        instance.BroadcastMessage(message);
    }
    
    /// <inheritdoc/>
    public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        SetUnityTimeScale(distributedMessage.Content.PopFloat());
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        messagesManager?.UnicastMessage(endPoint, distributedMessage);
    }

    /// <inheritdoc/>
    public void BroadcastMessage(DistributedMessage distributedMessage)
    {
        messagesManager?.BroadcastMessage(distributedMessage);
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        var message = MessagesPool.Instance.GetMessage(4);
        message.Content.PushFloat(Time.timeScale);
        message.AddressKey = instance.Key;
        message.Type = DistributedMessageType.ReliableOrdered;
        UnicastMessage(endPoint, message);
    }
}