/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Connection
{
    using System;

    /// <summary>
    /// Class calculating mocked udp traffic
    /// </summary>
    public static class NetworkStatistics
    {
        /// <summary>
        /// Sending packets statistic
        /// </summary>
        public class SendingStatistic
        {
            /// <summary>
            /// Average bandwidth usage since start (in kilobytes/sec)
            /// </summary>
            public double AverageBandwidthUsage { get; set; }
            
            /// <summary>
            /// Size of all packets sent in last second (in kilobytes)
            /// </summary>
            public int PacketsSizeSentLastSecond { get; set; }
            
            /// <summary>
            /// Size of last packet sent (in bytes)
            /// </summary>
            public int LastPacketSize { get; set; }
        }

        /// <summary>
        /// Last sending packets statistic
        /// </summary>
        private static readonly SendingStatistic SendingStat = new SendingStatistic();
        
        /// <summary>
        /// Size of all packages sent while statistics were enabled
        /// </summary>
        private static int sentPackagesSize;
        
        /// <summary>
        /// Time of first package sent while statistics were enabled
        /// </summary>
        private static DateTime firstSentPackageTime = DateTime.MinValue;
        
        /// <summary>
        /// Size of packages sent in the last recorded second
        /// </summary>
        private static int lastSecondSentPackagesSize;
        
        /// <summary>
        /// Time when recording last second started
        /// </summary>
        private static DateTime lastSecondStartTime;

        /// <summary>
        /// Event called when the sending statistic is updated
        /// </summary>
        public static event Action<SendingStatistic> SendingStatisticUpdated;

        /// <summary>
        /// Report sent package in order to update statistics (if enabled)
        /// </summary>
        public static void ReportSentPackage(int packageSize)
        {
            if (SendingStatisticUpdated == null)
                return;
            sentPackagesSize += packageSize;
            var currentTime = DateTime.Now;
            if (firstSentPackageTime == DateTime.MinValue)
            {
                firstSentPackageTime = currentTime;
                lastSecondStartTime = currentTime;
            }

            if ((currentTime - lastSecondStartTime).TotalSeconds <= 1.0f)
            {
                lastSecondSentPackagesSize += packageSize;
            }
            else
            {
                //Update sending statistic
                SendingStat.PacketsSizeSentLastSecond = lastSecondSentPackagesSize / 1024;
                var elapsedSeconds = (currentTime - firstSentPackageTime).TotalSeconds;
                var bandwidthUsage = (double) sentPackagesSize / 1024 / elapsedSeconds;
                SendingStat.AverageBandwidthUsage = bandwidthUsage;
                
                //Set nest second
                lastSecondStartTime = currentTime;
                lastSecondSentPackagesSize = packageSize;
            }
            SendingStat.LastPacketSize = packageSize;
            SendingStatisticUpdated.Invoke(SendingStat);
        }
    }
}