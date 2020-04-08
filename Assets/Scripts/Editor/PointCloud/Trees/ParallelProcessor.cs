/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.Threading;
    using Simulator.PointCloud.Trees;

    public abstract class ParallelProcessor
    {
        /// <summary>
        /// Describes work status on a processor.
        /// </summary>
        public enum WorkStatus
        {
            /// No data is currently being processed.
            Idle,

            /// Work has been queued and processing will start soon.
            Queued,

            /// Data is currently being processed.
            Busy
        }
        
        protected readonly object scheduleLock = new object();
        
        protected bool cancelFlag;
        
        protected WorkStatus workStatus = WorkStatus.Idle;

        /// <summary>
        /// Record with metadata of the node.
        /// </summary>
        public NodeRecord NodeRecord { get; private set; }

        /// <summary>
        /// Describes current work status of this processor.
        /// </summary>
        public WorkStatus Status
        {
            get
            {
                lock (scheduleLock)
                {
                    return workStatus;
                }
            }
        }

        /// <summary>
        /// Assigns node record for processing to this instance. Only valid if <see cref="ParallelProcessor.Status"/> is <see cref="ParallelProcessor.WorkStatus.Idle"/>.
        /// </summary>
        /// <param name="record">Node record to be processed.</param>
        /// <exception cref="Exception">processor is currently busy with other work.</exception>
        public void AssignWork(NodeRecord record)
        {
            lock (scheduleLock)
            {
                if (workStatus != WorkStatus.Idle)
                    throw new Exception("Processor cannot accept new work when it's busy!");

                workStatus = WorkStatus.Queued;
                NodeRecord = record;
            }
        }
        
        /// <summary>
        /// Starts work loop. It will be running until <see cref="StopWork"/> is called.
        /// </summary>
        public void StartWork()
        {
            while (!cancelFlag)
            {
                if (workStatus == WorkStatus.Queued)
                {
                    lock (scheduleLock)
                        workStatus = WorkStatus.Busy;

                    DoWorkInternal(NodeRecord);

                    lock (scheduleLock)
                        workStatus = WorkStatus.Idle;
                }
                else
                    Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Stops work loop.
        /// </summary>
        public void StopWork()
        {
            cancelFlag = true;
        }

        /// <summary>
        /// Method called whenever new processing begins.
        /// </summary>
        protected abstract void DoWorkInternal(NodeRecord record);
    }
}
