/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo
{
    using System.Collections.Generic;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Manager for caching VSE actions that can be reverted
    /// </summary>
    public class ScenarioUndoManager : MonoBehaviour
    {
        /// <summary>
        /// Maximum number of the undo records that can be undone
        /// </summary>
        private const int CacheLimit = 50;
        
        /// <summary>
        /// Cached undo records that can be undone
        /// </summary>
        private readonly LinkedList<UndoRecord> recordsCache = new LinkedList<UndoRecord>();

        /// <summary>
        /// Register new undo action record that can be undone
        /// </summary>
        /// <param name="record">Record that can be undone</param>
        public void RegisterRecord(UndoRecord record)
        {
            if (recordsCache.Count >= CacheLimit)
            {
                var lastRecord = recordsCache.Last.Value;
                lastRecord.Dispose();
                recordsCache.RemoveLast();
            }
            recordsCache.AddFirst(record);
        }

        /// <summary>
        /// Undo the last action record
        /// </summary>
        public void Undo()
        {
            var lastRecord = recordsCache.First?.Value;
            if (lastRecord == null)
                return;
            lastRecord.Undo();
            recordsCache.RemoveFirst();
            ScenarioManager.Instance.IsScenarioDirty = true;
        }

        /// <summary>
        /// Clear all the undo records as they are no longer valid
        /// </summary>
        public void ClearRecords()
        {
            recordsCache.Clear();
        }
    }
}
