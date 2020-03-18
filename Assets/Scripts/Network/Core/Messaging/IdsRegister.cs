/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text;

	using Connection;

	using Data;

	using Identification;

	/// <summary>
	/// Register which associate <see cref="IIdentifiedObject"/> objects with unique id, may assign new ids
	/// </summary>
	public class IdsRegister : IMessageReceiver, IMessageSender
	{
		/// <summary>
		/// Should this register assigns ids if checked object has no identifier
		/// </summary>
		private readonly bool assignIds;

		/// <summary>
		/// <see cref="MessagesManager"/> where internal messages will be forwarded
		/// </summary>
		private readonly MessagesManager messagesManager;

		/// <summary>
		/// Identifiers manager for all identified objects
		/// </summary>
		private readonly IIdManager idManager = new SimpleIdManager();

		/// <summary>
		/// Dictionary for a quick lookup which object is bound to an identifier
		/// </summary>
		private readonly Dictionary<int, IIdentifiedObject> idToObjectDictionary =
			new Dictionary<int, IIdentifiedObject>();

		/// <summary>
		/// Dictionary for a quick lookup what identifier is bound to the address key
		/// </summary>
		private readonly Dictionary<string, int> keyToIdDictionary =
			new Dictionary<string, int>();

		/// <summary>
		/// Dictionary with timestamps of the id registration
		/// </summary>
		private readonly Dictionary<int, DateTime> idRegistrationTimestamp = new Dictionary<int, DateTime>();

		/// <summary>
		/// Instantiates identified objects that waits for Key-Id binding
		/// </summary>
		private readonly List<IIdentifiedObject> unboundObjects = new List<IIdentifiedObject>();

		/// <summary>
		/// Key-Id bindings that will be used when proper identified object tries to register
		/// </summary>
		private readonly Dictionary<string, int> awaitingKeyIdBinds = new Dictionary<string, int>();

		/// <summary>
		/// Has this register already bound internal id
		/// </summary>
		private bool isInternalIdBound;

		/// <summary>
		/// Bytes count used to encode the identifier
		/// </summary>
		private const int BytesPerId = 4;

		/// <summary>
		/// Bytes required for command type 
		/// </summary>
		private static readonly int BytesPerCommandType =
			ByteCompression.RequiredBytes<IdsRegisterCommandType>();

		/// <inheritdoc/>
		public string Key { get; } = "IdsRegister";

		/// <summary>
		/// Date time of binding internal id
		/// </summary>
		public DateTime InternalIdBindUtcTime { get; set; } = DateTime.MinValue;

		/// <summary>
		/// Should this register assigns ids if checked object has no identifier
		/// </summary>
		public bool AssignIds => assignIds;

		/// <summary>
		/// Event invoked when new identified object is bound to identifier
		/// </summary>
		public event Action<IIdentifiedObject, int> ObjectBoundToId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="messagesManager"><see cref="MessagesManager"/> where internal messages will be forwarded</param>
		/// <param name="assignIds">Should this register assigns ids if checked object has no identifier</param>
		public IdsRegister(MessagesManager messagesManager, bool assignIds)
		{
			this.messagesManager = messagesManager;
			this.assignIds = assignIds;
		}

		/// <summary>
		/// Initialize own id if this register instance assigns ids
		/// </summary>
		public void SelfRegister()
		{
			if (!AssignIds) return;

			var id = idManager.GetId();
			idToObjectDictionary.Add(id, this);
			keyToIdDictionary.Add(Key, id);
			isInternalIdBound = true;
			InternalIdBindUtcTime = DateTime.UtcNow;
			idRegistrationTimestamp.Add(id, InternalIdBindUtcTime);
			BroadcastMessage(GetCommandMessage(IdsRegisterCommandType.BindIdAndKey, this));
		}

		/// <summary>
		/// Revoke all registered identifiers if this register does not assigns ids
		/// </summary>
		public void RevokeIds()
		{
			if (AssignIds) return;

			//Mark all registered objects as unbound if object still exists
			lock (unboundObjects)
				foreach (var identifiedObject in idToObjectDictionary)
					if (identifiedObject.Value != null)
						unboundObjects.Add(identifiedObject.Value);
			idToObjectDictionary.Clear();
			keyToIdDictionary.Clear();
			awaitingKeyIdBinds.Clear();
			idRegistrationTimestamp.Clear();
			isInternalIdBound = false;
		}

		/// <summary>
		/// Creates initialization message if this register assigns ids
		/// </summary>
		/// <returns>Initialization message</returns>
		/// <exception cref="ArgumentException">Cannot create initial message in register which does not assign ids.</exception>
		private DistributedMessage GetInitializationMessage()
		{
			if (!assignIds)
				throw new ArgumentException("Cannot create initial message in register which does not assign ids.");

			var id = ResolveId(this);
			if (id == null) throw new ArgumentException("Usage of the unregistered identified object.");

			var message = MessagesPool.Instance.GetMessage(8 + BytesPerId + BytesStack.GetMaxByteCount(Key) + 4);
			message.AddressKey = Key;
			message.Content.PushLong(messagesManager.TimeManager.GetTimeDifference(InternalIdBindUtcTime));
			message.Content.PushInt(id.Value, BytesPerId);
			message.Content.PushString(Key);
			message.Content.PushInt((int)IdsRegisterCommandType.BindIdAndKey, BytesPerCommandType);
			message.Type = DistributedMessageType.ReliableOrdered;
			return message;
		}

		/// <summary>
		/// Gets ids register command for the identified object in the bytes stack message
		/// </summary>
		/// <param name="commandType">Command type</param>
		/// <param name="identifiedObject">Identified object which is command target</param>
		/// <returns>Register command in bytes stack</returns>
		private DistributedMessage GetCommandMessage(
			IdsRegisterCommandType commandType,
			IIdentifiedObject identifiedObject)
		{
			var id = ResolveId(identifiedObject);
			if (id == null) throw new ArgumentException("Usage of the unregistered identified object.");

			var message =
				MessagesPool.Instance.GetMessage(
					BytesPerId + BytesStack.GetMaxByteCount(identifiedObject.Key) + BytesPerCommandType);
			message.AddressKey = Key;
			message.Content.PushInt(id.Value, BytesPerId);
			message.Content.PushString(identifiedObject.Key);
			message.Content.PushInt((int)commandType, BytesPerCommandType);
			message.Type = DistributedMessageType.ReliableOrdered;
			return message;
		}

		/// <summary>
		/// Pop identifier from the bytes stack
		/// </summary>
		/// <param name="bytesStack">Bytes stack with the message</param>
		/// <returns>Object bound to the identifier, null if identifier is not bound</returns>
		public int PopId(BytesStack bytesStack)
		{
			return bytesStack.PopInt(BytesPerId);
		}

		/// <summary>
		/// Check if bytes stack contains internal id for the register
		/// </summary>
		/// <param name="sender">The peer from which message has been received</param>
		/// <param name="distributedMessage">Checked message</param>
		/// <returns>True if bytes stack contains internal id for register, false otherwise</returns>
		public bool IsInitializationMessage(IPeerManager sender, DistributedMessage distributedMessage)
		{
			if (isInternalIdBound) return false;

			//Check if this is not an initial message
			var command = (IdsRegisterCommandType)distributedMessage.Content.PeekInt(BytesPerCommandType);
			if (command != IdsRegisterCommandType.BindIdAndKey) return false;

			try
			{
				var offset = BytesPerCommandType;
				var key = distributedMessage.Content.PeekString(offset);
				if (Key == key)
				{
					offset += 4 + Encoding.UTF8.GetBytes(key).Length;
					var id = distributedMessage.Content.PeekInt(BytesPerId, offset);
					offset += BytesPerId;
					isInternalIdBound = true;
					var timeDifference = distributedMessage.Content.PeekLong(8, offset);
					InternalIdBindUtcTime = messagesManager.TimeManager.GetTimestamp(timeDifference);
					idRegistrationTimestamp.Add(id, InternalIdBindUtcTime);
					idToObjectDictionary.Add(id, this);
					keyToIdDictionary.Add(Key, id);
					ObjectBoundToId?.Invoke(this, id);
					return true;
				}
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}

			return false;
		}

		/// <summary>
		/// Search bound object to the identifier
		/// </summary>
		/// <param name="id">Identifier of the object</param>
		/// <returns>Object bound to the identifier, null if identifier is not bound</returns>
		public IIdentifiedObject ResolveObject(int id)
		{
			idToObjectDictionary.TryGetValue(id, out var identifiedObject);
			return identifiedObject;
		}

		/// <summary>
		/// Checks if object is already bound to any identifier
		/// </summary>
		/// <param name="identifiedObject">Identifier object to check</param>
		/// <returns></returns>
		public bool IsObjectBoundToId(IIdentifiedObject identifiedObject)
		{
			return IsKeyBoundToId(identifiedObject.Key);
		}

		/// <summary>
		/// Checks if address key is already bound to any identifier
		/// </summary>
		/// <param name="addressKey">Address key to check</param>
		/// <returns></returns>
		public bool IsKeyBoundToId(string addressKey)
		{
			return keyToIdDictionary.ContainsKey(addressKey);
		}

		/// <summary>
		/// Returns identifier of the identified object if it is already bound
		/// </summary>
		/// <param name="identifiedObject">Identified object to check</param>
		/// <returns>Identifier of the identified object, null if it's not bound</returns>
		public int? ResolveId(IIdentifiedObject identifiedObject)
		{
			return ResolveId(identifiedObject.Key);
		}

		/// <summary>
		/// Returns identifier of the address key if it is already bound
		/// </summary>
		/// <param name="addressKey">Address key of the identified object to check</param>
		/// <returns>Identifier bound to address key, null if it's not bound</returns>
		public int? ResolveId(string addressKey)
		{
			if (!keyToIdDictionary.TryGetValue(addressKey, out var id)) return null;

			return id;
		}

		/// <summary>
		/// Gets the timestamp when the id was registered
		/// </summary>
		/// <param name="id">Id of registered object</param>
		/// <returns>Timestamp when the id was registered</returns>
		public DateTime? GetRegistrationTimestamp(int id)
		{
			if (idRegistrationTimestamp.TryGetValue(id, out var registrationTimestamp)) return registrationTimestamp;

			return null;
		}

		/// <summary>
		/// Register new object
		/// </summary>
		/// <param name="newObject">Object to be registered</param>
		public void RegisterObject(IIdentifiedObject newObject)
		{
			if (keyToIdDictionary.ContainsKey(newObject.Key)) return;

			if (AssignIds)
			{
				var id = idManager.GetId();
				idToObjectDictionary.Add(id, newObject);
				keyToIdDictionary.Add(newObject.Key, id);
				idRegistrationTimestamp.Add(id, DateTime.UtcNow);
				BroadcastMessage(GetCommandMessage(IdsRegisterCommandType.BindIdAndKey, newObject));
				ObjectBoundToId?.Invoke(newObject, id);
				return;
			}

			lock (unboundObjects) unboundObjects.Add(newObject);
			if (!awaitingKeyIdBinds.TryGetValue(newObject.Key, out var bindId)) return;

			awaitingKeyIdBinds.Remove(newObject.Key);
			TryBindReceiver(newObject.Key, bindId);
		}

		/// <summary>
		/// Unregister identified object
		/// </summary>
		/// <param name="unregisteredObject">Object to be unregistered</param>
		public void UnregisterObject(IIdentifiedObject unregisteredObject)
		{
			if (unregisteredObject == null) return;

			if (!keyToIdDictionary.ContainsKey(unregisteredObject.Key))
			{
				lock (unboundObjects) unboundObjects.Remove(unregisteredObject);
				return;
			}

			var id = ResolveId(unregisteredObject);
			if (id != null)
			{
				if (AssignIds)
					BroadcastMessage(
						GetCommandMessage(
							IdsRegisterCommandType.UnbindIdAndKey,
							unregisteredObject));

				idRegistrationTimestamp.Remove(id.Value);
				idToObjectDictionary.Remove(id.Value);
				idManager.ReturnId(id.Value);
			}

			keyToIdDictionary.Remove(unregisteredObject.Key);
		}

		/// <summary>
		/// Unbinds Key-Id pair from current object
		/// </summary>
		/// <param name="key">Key to unbind</param>
		/// <param name="id">Id to unbind</param>
		public void UnbindKeyId(string key, int id)
		{
			if (awaitingKeyIdBinds.Remove(key)) return;

			//Check is there is corresponding receiver with same key
			lock (unboundObjects)
			{
				var unboundReceiver = unboundObjects.Find(r => r.Key == key);
				if (unboundReceiver != null)
				{
					unboundObjects.Remove(unboundReceiver);
					return;
				}
			}

			UnregisterObject(ResolveObject(id));
		}

		/// <summary>
		/// Bind unbound object with the Key-Id pair
		/// </summary>
		/// <param name="key">Key to bind</param>
		/// <param name="id">Id to bind</param>
		private void TryBindReceiver(string key, int id)
		{
			IIdentifiedObject objectToBind;

			//Check is there is corresponding identified object with same key
			lock (unboundObjects)
			{
				objectToBind = unboundObjects.Find(r => r.Key == key);
				if (objectToBind != null) unboundObjects.Remove(objectToBind);
			}

			//If corresponding identified object is found bind it to the id
			if (objectToBind != null)
			{
				idToObjectDictionary.Add(id, objectToBind);
				keyToIdDictionary.Add(objectToBind.Key, id);
				ObjectBoundToId?.Invoke(objectToBind, id);
			}

			//If there is no proper unbound identified object add key-id binding to the list
			else
			{
				if (!awaitingKeyIdBinds.TryGetValue(key, out var currentIdForKey)) awaitingKeyIdBinds.Add(key, id);
				else if (currentIdForKey != id)
				{
					//Id changed
					awaitingKeyIdBinds.Remove(key);
					awaitingKeyIdBinds.Add(key, id);
				}
			}
		}

		/// <inheritdoc/>
		public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
		{
			var command =
				(IdsRegisterCommandType)distributedMessage.Content.PopInt(
					ByteCompression
						.RequiredBytes<IdsRegisterCommandType>());
			var key = distributedMessage.Content.PopString();
			var id = distributedMessage.Content.PopInt(BytesPerId);
			IIdentifiedObject registeredObject;
			int awaitingId;
			switch (command)
			{
				case IdsRegisterCommandType.BindIdAndKey:
					if (AssignIds) return;

					//Check if object is already registered
					if ((idToObjectDictionary.TryGetValue(id, out registeredObject) && registeredObject.Key == key) ||
					    (awaitingKeyIdBinds.TryGetValue(key, out awaitingId) && awaitingId == id)) return;

					//New bind to the id received before receiving unbind command
					if (registeredObject != null)
					{
						UnbindKeyId(key, id);
						idRegistrationTimestamp.Remove(id);
					}

					idRegistrationTimestamp.Add(id, distributedMessage.ServerTimestamp);
					TryBindReceiver(key, id);
					break;
				case IdsRegisterCommandType.UnbindIdAndKey:
					if (AssignIds) return;

					//Remove awaiting binding if it is available
					if (awaitingKeyIdBinds.TryGetValue(key, out awaitingId) && awaitingId == id)
					{
						awaitingKeyIdBinds.Remove(key);
						idRegistrationTimestamp.Remove(id);
						return;
					}

					//Check if object have not been unbounded already
					if (!idToObjectDictionary.TryGetValue(id, out registeredObject)) return;

					//Check if bound object has the same key
					if (registeredObject.Key != key) return;

					UnbindKeyId(key, id);
					idRegistrationTimestamp.Remove(id);
					break;
			}
		}

		/// <summary>
		/// Pushes to the message identifier bound to address key in the message
		/// </summary>
		/// <param name="distributedMessage">Message where identifier will be pushed</param>
		/// <exception cref="ArgumentException">Cannot resolve identifier for this address key</exception>
		public void PushId(DistributedMessage distributedMessage)
		{
			if (string.IsNullOrEmpty(distributedMessage.AddressKey))
				throw new ArgumentException("Cannot send message with empty address key.");

			var id = ResolveId(distributedMessage.AddressKey);
			if (id == null)
				throw new ArgumentException(
					$"Cannot resolve identifier for address key {distributedMessage.AddressKey}. Check if key is bound to identifier calling this method.");

			distributedMessage.Content.PushInt(id.Value, BytesPerId);
		}

		/// <inheritdoc/>
		public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
		{
			messagesManager.UnicastMessage(endPoint, distributedMessage);
		}

		/// <inheritdoc/>
		public void BroadcastMessage(DistributedMessage distributedMessage)
		{
			messagesManager.BroadcastMessage(distributedMessage);
		}

		/// <inheritdoc/>
		public void UnicastInitialMessages(IPEndPoint endPoint)
		{
			if (!AssignIds) return;

			//Send registered objects
			foreach (var idToObject in idToObjectDictionary)
			{
				UnicastMessage(
					endPoint,
					idToObject.Value == this
						? GetInitializationMessage()
						: GetCommandMessage(IdsRegisterCommandType.BindIdAndKey, idToObject.Value));
			}
		}
	}
}
