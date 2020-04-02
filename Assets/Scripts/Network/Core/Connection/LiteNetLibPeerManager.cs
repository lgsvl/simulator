/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Connection
{
	using System.Net;

	using LiteNetLib;

	using Messaging;
	using Messaging.Data;

	/// <summary>
	/// Peer to peer connection manager using LiteNetLib
	/// </summary>
	public class LiteNetLibPeerManager : IPeerManager
	{
		/// <summary>
		/// Managed NetPeer
		/// </summary>
		public NetPeer Peer { get; }

		/// <inheritdoc/>
		public long RemoteTimeTicksDifference => Peer.RemoteTimeDelta;

		/// <inheritdoc/>
		public bool Connected => Peer.ConnectionState == ConnectionState.Connected;

		/// <inheritdoc/>
		public IPEndPoint PeerEndPoint => Peer.EndPoint;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peer">Managed NetPeer</param>
		public LiteNetLibPeerManager(NetPeer peer)
		{
			Peer = peer;
		}

		/// <inheritdoc/>
		public void Disconnect()
		{
			Peer.Disconnect();
		}

		/// <inheritdoc/>
		public void Send(DistributedMessage distributedMessage)
		{
			var bytesStack = distributedMessage.Content;
			try
			{
				NetworkStatistics.ReportSentPackage(bytesStack.Count);
				Peer.Send(bytesStack.RawData, 0, bytesStack.Count, GetDeliveryMethod(distributedMessage.Type));
			}
			catch (TooBigPacketException)
			{
				Log.Error($"Too large message to be sent: {bytesStack.Count}.");
			}
		}

		/// <summary>
		/// Gets LiteNetLib DeliveryMethod corresponding to the given MessageType
		/// </summary>
		/// <param name="distributedMessageType">Message type</param>
		/// <returns>Corresponding LiteNetLib DeliveryMethod to the given MessageType</returns>
		public static DeliveryMethod GetDeliveryMethod(DistributedMessageType distributedMessageType)
		{
			return (DeliveryMethod)distributedMessageType;
		}
	}
}
