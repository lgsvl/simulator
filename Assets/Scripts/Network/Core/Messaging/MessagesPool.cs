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

	using Simulator.Network.Core.Messaging.Data;

	/// <summary>
	/// Distributed messages pool
	/// </summary>
	public class MessagesPool
	{
		/// <summary>
		/// Bytes count required for meta data
		/// </summary>
		private const int MetaDataBytes = 8;
		
		/// <summary>
		/// Global instance of the messages pool
		/// </summary>
		public static MessagesPool Instance { get; } = new MessagesPool();

		/// <summary>
		/// Scope limits of the inner pools in bytes (every pool use different scope)
		/// </summary>
		public readonly int[] PoolsScopes = { 128, 256, 1024, 8192};

		/// <summary>
		/// Function called when new message should be created
		/// </summary>
		public readonly Func<DistributedMessage> MessageCreator = null;

		/// <summary>
		/// Messages pools
		/// </summary>
		private List<DistributedMessage>[] pools;

		/// <summary>
		/// Constructor
		/// </summary>
		public MessagesPool()
		{
			InitPools();
		}

		/// <summary>
		/// Initializes the pools
		/// </summary>
		private void InitPools()
		{
			pools = new List<DistributedMessage>[PoolsScopes.Length];
			for (var i = 0; i < PoolsScopes.Length; i++) pools[i] = new List<DistributedMessage>();
		}

		/// <summary>
		/// Calculates pool index basing on the bytes count
		/// </summary>
		/// <param name="bytes">Required bytes count</param>
		/// <returns>Pool index corresponding to the bytes count</returns>
		private int BytesToIndex(int bytes)
		{
			bytes += MetaDataBytes; //Add bytes required for metadata
			for (var i = 0; i < PoolsScopes.Length; i++)
				if (bytes < PoolsScopes[i]) return i;

			return PoolsScopes.Length - 1;
		}

		/// <summary>
		/// Creates new message object
		/// </summary>
		/// <returns>New message object</returns>
		private DistributedMessage CreateNewMessage()
		{
			return MessageCreator == null ? new DistributedMessage(new BytesStack()) : MessageCreator.Invoke();
		}

		/// <summary>
		/// Gets message from any pool
		/// </summary>
		/// <returns>Message</returns>
		private DistributedMessage GetMessageFromAnyPool()
		{
			lock (pools)
			{
				for (var i = 0; i < pools.Length; i++)
				{
					var poolSize = pools[i].Count;
					if (poolSize == 0) continue;
					
					var message = pools[i][poolSize-1];
					pools[i].RemoveAt(poolSize-1);
					return message;
				}
			}

			return CreateNewMessage();
		}

		/// <summary>
		/// Gets message from selected pool
		/// </summary>
		/// <param name="poolIndex">Index from which message will be get</param>
		/// <returns>Message</returns>
		private DistributedMessage GetMessageFromPool(int poolIndex)
		{
			lock (pools)
			{
				var poolSize = pools[poolIndex].Count;
				if (poolSize == 0) return CreateNewMessage();
				
				var message = pools[poolIndex][poolSize-1];
				pools[poolIndex].RemoveAt(poolSize-1);
				return message;
			}
		}

		/// <summary>
		/// Gets message from a pool
		/// </summary>
		/// <param name="bytes">Expected message's content size</param>
		/// <returns>Message</returns>
		public DistributedMessage GetMessage(int bytes = 0)
		{
			var message = bytes<=0 ? GetMessageFromAnyPool() : GetMessageFromPool(BytesToIndex(bytes));
			message.OriginPool = this;
			return message;
		}

		/// <summary>
		/// Return message to the pool
		/// </summary>
		/// <param name="message">Message to be returned</param>
		public void ReleaseMessage(DistributedMessage message)
		{
			if (message.OriginPool != this) return;
			var poolIndex = BytesToIndex(message.Content.StackSize);
			message.OriginPool = null;
			lock (pools)
			{
				pools[poolIndex].Add(message);
			}
		}

		/// <summary>
		/// Counts available messages in the pool
		/// </summary>
		/// <returns>Count of available messages in the pool</returns>
		public int CountAvailableMessages()
		{
			var poolSize = 0;
			lock (pools)
			{
				for (var i = 0; i < pools.Length; i++) poolSize += pools[i].Count;
			}

			return poolSize;
		}
	}
}
