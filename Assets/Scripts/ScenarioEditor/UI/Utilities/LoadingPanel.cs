/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Utilities
{
    using System;
    using System.Collections.Generic;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Panel which displays loading process for all the VSE operations
    /// </summary>
    public class LoadingPanel : MonoBehaviour
    {
        /// <summary>
        /// A single loading process class, disables hiding loading panel until completed
        /// </summary>
        public class LoadingProcess
        {
            /// <summary>
            /// UI Text representing this loading process
            /// </summary>
            public Text LoadingText { get; }
            
            /// <summary>
            /// Is this loading process completed
            /// </summary>
            public bool IsCompleted { get; private set; }

            /// <summary>
            /// Event invoked when the process becomes completed
            /// </summary>
            public event Action Completed;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="loadingText">UI Text representing this loading process</param>
            public LoadingProcess(Text loadingText)
            {
                LoadingText = loadingText;
                Update("");
            }
            
            /// <summary>
            /// Updates the loading process
            /// </summary>
            /// <param name="text">Text that will be displayed</param>
            public void Update(string text)
            {
                if (LoadingText!=null)
                    LoadingText.text = text;
            }

            /// <summary>
            /// Marks progress as completed and notifies listeners
            /// </summary>
            public void NotifyCompletion()
            {
                IsCompleted = true;
                if (IsCompleted)
                    Completed?.Invoke();
            }
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Transform where all process panels will be parented
        /// </summary>
        [SerializeField]
        private Transform processesParent;
        
        /// <summary>
        /// Sample text displaying the loading process
        /// </summary>
        [SerializeField]
        private Text processTextSample;
#pragma warning restore 0649

        /// <summary>
        /// Semaphore that holds the loading screen
        /// </summary>
        private readonly List<LoadingProcess> processes = new List<LoadingProcess>();


        /// <summary>
        /// Adds new loading process to the panel
        /// </summary>
        public LoadingProcess AddProgress()
        {
            var textObject = ScenarioManager.Instance.prefabsPools.GetInstance(processTextSample.gameObject);
            textObject.transform.SetParent(processesParent);
            textObject.SetActive(true);
            var text = textObject.GetComponent<Text>();
            var process = new LoadingProcess(text);
            process.Completed += OnProcessComplete;
            processes.Add(process);
            gameObject.SetActive(true);
            return process;
        }

        /// <summary>
        /// Hides loading panel
        /// </summary>
        public void Hide()
        {
            var prefabsPool = ScenarioManager.Instance.prefabsPools;
            gameObject.SetActive(false);
            for (var i = 0; i < processes.Count; i++)
            {
                processes[i].Completed -= OnProcessComplete;
                prefabsPool.ReturnInstance(processes[i].LoadingText.gameObject);
            }

            processes.Clear();
        }

        /// <summary>
        /// Checks if all processes have been completed, if true hides this panel
        /// </summary>
        private void OnProcessComplete()
        {
            for (var i = 0; i < processes.Count; i++)
            {
                if (!processes[i].IsCompleted)
                    return;
            }

            Hide();
        }
    }
}