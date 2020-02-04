/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Net;
using Simulator;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
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
    /// Count of the time scale locks, unlock the time scale when equals 0
    /// </summary>
    public int TimeScaleLocks { get; private set; }

    /// <summary>
    /// Is the time scale currently locked to 0.0f
    /// </summary>
    public bool IsTimeScaleLocked => TimeScaleLocks > 0;
    
    /// <summary>
    /// Is the time scale currently unlocked, set time scale is applied
    /// </summary>
    public bool IsTimeScaleUnlocked => TimeScaleLocks == 0;

    /// <summary>
    /// Time scale which is applied when time scale is unlocked
    /// </summary>
    public float TimeScale
    {
        get => IsTimeScaleUnlocked ? timeScale : 0.0f;
        set
        {
            timeScale = value;
            if (IsTimeScaleUnlocked) 
                SetUnityTimeScale(timeScale);
        }
    }

    /// <summary>
    /// Event invoked when the time scale gets locked
    /// </summary>
    public event Action TimeScaleLocked;

    /// <summary>
    /// Event invoked when the time scale gets unlocked
    /// </summary>
    public event Action TimeScaleUnlocked;
    
    /// <summary>
    /// Event invoked when the count of time scale locks changes
    /// </summary>
    public event Action<float> TimeScaleLocksChanged;

    /// <summary>
    /// Initialization method required to distribute the timescale
    /// </summary>
    /// <param name="messagesManager">Messages manager handling the distributed messages</param>
    protected internal void Initialize(MessagesManager messagesManager)
    {
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
        messagesManager?.UnregisterObject(this);
        messagesManager = null;
        instance = null;
    }

    /// <summary>
    /// Raises the time scale lock by one, time scale is set to 0.0f while locked
    /// </summary>
    public void LockTimeScale()
    {
        if (TimeScaleLocks++ != 0)
        {
            TimeScaleLocksChanged?.Invoke(TimeScaleLocks);
            return;
        }
        SetUnityTimeScale(0.0f);
        TimeScaleLocked?.Invoke();
        TimeScaleLocksChanged?.Invoke(TimeScaleLocks);
    }

    /// <summary>
    /// Lowers the time scale lock by one, time scale value is applied when gets unlocked
    /// </summary>
    public void UnlockTimeScale()
    {
        if (TimeScaleLocks == 0)
        {
            Debug.LogWarning("Trying to unlock already unlocked time scale.");
        }
        else if (--TimeScaleLocks == 0)
        {
            SetUnityTimeScale(TimeScale);
            TimeScaleUnlocked?.Invoke();
        }
        TimeScaleLocksChanged?.Invoke(TimeScaleLocks);
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
        var content = new BytesStack();
        content.PushFloat(Time.timeScale);
        instance.BroadcastMessage(new Message(instance.Key, content, MessageType.ReliableOrdered));
    }
    
    /// <inheritdoc/>
    public void ReceiveMessage(IPeerManager sender, Message message)
    {
        SetUnityTimeScale(message.Content.PopFloat());
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, Message message)
    {
        messagesManager?.UnicastMessage(endPoint, message);
    }

    /// <inheritdoc/>
    public void BroadcastMessage(Message message)
    {
        messagesManager?.BroadcastMessage(message);
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        var content = new BytesStack();
        content.PushFloat(Time.timeScale);
        UnicastMessage(endPoint, new Message(Key, content, MessageType.ReliableOrdered));
    }
}