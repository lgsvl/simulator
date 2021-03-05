/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Manager for caching VSE actions that can be reverted
    /// </summary>
    public class ScenarioUndoManager : MonoBehaviour, IScenarioEditorExtension
    {
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Maximum number of the undo records that can be undone
        /// </summary>
        private const int CacheLimit = 50;
        
        /// <summary>
        /// Cached undo records that can be undone
        /// </summary>
        private readonly LinkedList<UndoRecord> recordsCache = new LinkedList<UndoRecord>();

        /// <inheritdoc/>
        public Task Initialize()
        {
            if (IsInitialized)
                return Task.CompletedTask;
            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
            gameObject.SetActive(false);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }

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
            try
            {
                lastRecord.Undo();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Undo failed for a {lastRecord.GetType().Name}. Exception: {ex.Message}. Callstack: {ex.StackTrace}.");
            }

            recordsCache.RemoveFirst();
            ScenarioManager.Instance.IsScenarioDirty = true;
        }

        /// <summary>
        /// Clear all the undo records as they are no longer valid
        /// </summary>
        public void ClearRecords()
        {
            foreach (var undoRecord in recordsCache) undoRecord.Dispose();
            recordsCache.Clear();
        }
    }
}
