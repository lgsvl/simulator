/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Tests.Editor.Shared.Messaging.Data
{
	using NUnit.Framework;

	using Simulator.Network.Core.Messaging;

	/// <summary>
	/// Tests for the <see cref="MessagesPool"/> class
	/// </summary>
	[TestFixture]
	public class MessagesPoolTests
	{
		/// <summary>
		/// Tests getting message from the pool and releasing it
		/// </summary>
		[Test]
		public void GetAndReleaseTest()
		{
			var messagesPool = new MessagesPool();
			var message1 = messagesPool.GetMessage();
			var message2 = messagesPool.GetMessage();
			Assert.True(message1!=null && message2 != null);
			Assert.True(message1.Content.Count == 0 && message2.Content.Count == 0);
			Assert.True(message1.OriginPool == messagesPool && message2.OriginPool == messagesPool);
			Assert.True(messagesPool.CountAvailableMessages() == 0);
			message1.Content.PushString("Test1Value");
			message2.Content.PushString("Test2Value");
			Assert.True(message1.Content.Count > 0);
			message1.Release();
			Assert.True(messagesPool.CountAvailableMessages() == 1);
			Assert.True(message1.Content.Count == 0);
			Assert.True(message1.OriginPool == null);
			Assert.True(message2.Content.Count > 0);
			Assert.True(message2.OriginPool != null);
			Assert.True(message2.Content.PeekString() == "Test2Value");
			message2.Release();
			Assert.True(messagesPool.CountAvailableMessages() == 2);
			Assert.True(message2.Content.Count == 0);
			Assert.True(message2.OriginPool == null);
			messagesPool.GetMessage();
			Assert.True(messagesPool.CountAvailableMessages() == 1);
			messagesPool.GetMessage();
			Assert.True(messagesPool.CountAvailableMessages() == 0);
		}
	}
}
