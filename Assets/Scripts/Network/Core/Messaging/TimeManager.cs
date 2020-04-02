/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging
{
    using System;
    using Data;

    /// <summary>
    /// Network time manager for handling UTC timestamps in the messages
    /// </summary>
    public class TimeManager
    {
        /// <summary>
        /// Epoch time used to compress time to integer
        /// </summary>
        private static DateTime epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Calculates difference between current time and epoch time
        /// </summary>
        public long CurrentTicksDifference => GetTimeDifference(DateTime.UtcNow);
        
        /// <summary>
        /// Pushes current time difference from epoch time to the message (in milliseconds)
        /// </summary>
        /// <param name="distributedMessage">Message where time difference will be pushed</param>
        public void PushTimeDifference(DistributedMessage distributedMessage)
        {
            distributedMessage.TimeTicksDifference = CurrentTicksDifference;
            distributedMessage.Content.PushLong(distributedMessage.TimeTicksDifference);
        }

        /// <summary>
        /// Pops and sets the time difference (in ticks) and the timestamp in the message
        /// </summary>
        /// <param name="distributedMessage">Message with the time difference in the content</param>
        /// <param name="remoteTimeTicksDifference">Difference between local time and remote time in ticks count</param>
        public void PopTimeDifference(DistributedMessage distributedMessage, long remoteTimeTicksDifference)
        {
            var epochTimeDifference = distributedMessage.Content.PopLong();
            distributedMessage.TimeTicksDifference = epochTimeDifference;
            distributedMessage.ServerTimestamp = GetTimestamp(epochTimeDifference);
            distributedMessage.Timestamp = GetTimestamp(epochTimeDifference-remoteTimeTicksDifference);
        }

        /// <summary>
        /// Calculates the timestamp based on the epoch time and time difference, check if epoch time is set before use
        /// </summary>
        /// <param name="timeDifference">Time difference to the epoch time in ticks</param>
        /// <returns>Timestamp based on the epoch time and time difference</returns>
        public DateTime GetTimestamp(long timeDifference)
        {
            return epoch.AddTicks(timeDifference);
        }

        /// <summary>
        /// Calculates time difference in ticks between epoch and the given date time
        /// </summary>
        /// <param name="dateTime">Date time for which time difference will be calculated</param>
        /// <returns>Time difference in ticks between epoch and the given date time</returns>
        public long GetTimeDifference(DateTime dateTime)
        {
            return (dateTime - epoch).Ticks;
        }
    }
}
