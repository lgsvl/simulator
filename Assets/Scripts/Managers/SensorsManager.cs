/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using System.Net;

using Simulator;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Sensors;
using Simulator.Sensors.Postprocessing;

public class SensorsManager : IMessageSender, IMessageReceiver
{
	class SensorMetaData
	{
		private string hierarchyPath;

		public SensorBase Instance { get; set; }

		public string UID { get; set; }

		public IPEndPoint HostEndPoint;

		public string HierarchyPath =>
			hierarchyPath ?? (hierarchyPath = HierarchyUtilities.GetPath(Instance.transform));

		public SensorMetaData(SensorBase instance)
		{
			Instance = instance;
		}
	}

	public string Key { get; } = "SensorsManager";

	public SensorPostProcessSystem PostProcessSystem { get; private set; }

	private List<SensorMetaData> sensors = new List<SensorMetaData>();

	private Dictionary<SensorBase, SensorMetaData> instanceToMetaData = new Dictionary<SensorBase, SensorMetaData>();

	private Dictionary<string, SensorMetaData> uidToMetaData = new Dictionary<string, SensorMetaData>();

	private Dictionary<string, string> awaitingPathToUID = new Dictionary<string, string>();

	public void Initialize()
	{
		Loader.Instance.Network.MessagesManager?.RegisterObject(this);
		PostProcessSystem = new SensorPostProcessSystem();
		PostProcessSystem.Initialize();
	}

	public void Deinitialize()
	{
		ClearSensorsRegistry();
		Loader.Instance.Network.MessagesManager?.UnregisterObject(this);
		PostProcessSystem?.Deinitialize();
	}

	public void RegisterSensor(SensorBase sensorBase)
	{
		var metaData = new SensorMetaData(sensorBase);
		sensors.Add(metaData);
		instanceToMetaData.Add(sensorBase, metaData);
		if (awaitingPathToUID.Count > 0 && awaitingPathToUID.TryGetValue(metaData.HierarchyPath, out var uid))
		{
			awaitingPathToUID.Remove(metaData.HierarchyPath);
			uidToMetaData.Add(uid, metaData);
		}
	}

	public void UnregisterSensor(SensorBase sensorBase)
	{
		if (!instanceToMetaData.TryGetValue(sensorBase, out var metaData)) return;

		instanceToMetaData.Remove(sensorBase);
		var keyValuePairs = uidToMetaData.Where(keyValuePair => keyValuePair.Value == metaData).ToArray();
		foreach (var keyValuePair in keyValuePairs) uidToMetaData.Remove(keyValuePair.Key);
		sensors.Remove(metaData);
	}

	public void ClearSensorsRegistry()
	{
		sensors.Clear();
		instanceToMetaData.Clear();
		uidToMetaData.Clear();
		awaitingPathToUID.Clear();
	}

	public bool AppendUid(SensorBase sensorBase, string uid)
	{
		if (!instanceToMetaData.TryGetValue(sensorBase, out var metaData)) return false;

		metaData.UID = uid;
		uidToMetaData.Add(uid, metaData);
		if (Loader.Instance.Network.IsMaster)
		{
			var message =
				MessagesPool.Instance.GetMessage(BytesStack.GetMaxByteCount(metaData.UID) +
				                                 BytesStack.GetMaxByteCount(metaData.HierarchyPath));
			message.AddressKey = Key;
			message.Content.PushString(metaData.UID);
			message.Content.PushString(metaData.HierarchyPath);
			message.Type = DistributedMessageType.ReliableOrdered;
			BroadcastMessage(message);
		}

		return true;
	}

	public bool AppendEndPoint(SensorBase sensorBase, IPEndPoint endPoint)
	{
		if (!instanceToMetaData.TryGetValue(sensorBase, out var metaData)) return false;

		metaData.HostEndPoint = endPoint;
		return true;
	}

	public string GetSensorUid(SensorBase sensorBase)
	{
		return instanceToMetaData.TryGetValue(sensorBase, out var metaData) ? metaData.UID : null;
	}

	public SensorBase GetSensor(string uid)
	{
		return uidToMetaData.TryGetValue(uid, out var metaData) ? metaData.Instance : null;
	}

	public IPEndPoint GetHostEndPoint(SensorBase sensorBase)
	{
		return instanceToMetaData.TryGetValue(sensorBase, out var metaData) ? metaData.HostEndPoint : null;
	}

	public IPEndPoint GetHostEndPoint(string uid)
	{
		return uidToMetaData.TryGetValue(uid, out var metaData) ? metaData.HostEndPoint : null;
	}

	public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
	{
		var sensorPath = distributedMessage.Content.PopString();
		var sensorUid = distributedMessage.Content.PopString();
		var sensorInPath = sensors.FirstOrDefault(metaData => metaData.HierarchyPath == sensorPath);
		if (sensorInPath != null) AppendUid(sensorInPath.Instance, sensorUid);
		else awaitingPathToUID.Add(sensorPath, sensorUid);
	}

	public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
	{
		Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, distributedMessage);
	}

	public void BroadcastMessage(DistributedMessage distributedMessage)
	{
		Loader.Instance.Network.MessagesManager?.BroadcastMessage(distributedMessage);
	}

	public void UnicastInitialMessages(IPEndPoint endPoint)
	{
		foreach (var sensorMetaData in sensors)
		{
			if (Equals(sensorMetaData.HostEndPoint, endPoint) && !string.IsNullOrEmpty(sensorMetaData.UID))
			{
				var message = MessagesPool.Instance.GetMessage(
					BytesStack.GetMaxByteCount(sensorMetaData.UID) +
					BytesStack.GetMaxByteCount(sensorMetaData.HierarchyPath));
				message.AddressKey = Key;
				message.Content.PushString(sensorMetaData.UID);
				message.Content.PushString(sensorMetaData.HierarchyPath);
				message.Type = DistributedMessageType.ReliableOrdered;
				BroadcastMessage(message);
			}
		}
	}
}
