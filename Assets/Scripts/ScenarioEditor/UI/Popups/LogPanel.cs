/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapSelecting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Log panel for displaying message to the user for a limited time
    /// </summary>
    public class LogPanel : MonoBehaviour
    {
        /// <summary>
        /// Type of the viewed log
        /// </summary>
        public enum LogType
        {
            /// <summary>
            /// Just information for the user
            /// </summary>
            Info,
            
            /// <summary>
            /// Warning that was handled automatically
            /// </summary>
            Warning,
            
            /// <summary>
            /// Error that cannot be handled by the code
            /// </summary>
            Error
        }
        
        /// <summary>
        /// Single log data that will be displayed in the panel
        /// </summary>
        public class LogData
        {
            /// <summary>
            /// Type of the log
            /// </summary>
            public LogType Type { get; set; }
            
            /// <summary>
            /// Log text that will be displayed
            /// </summary>
            public string Text { get; set; }
            
            /// <summary>
            /// Color of the displayed text
            /// </summary>
            public Color TextColor { get; set; } = Color.black;
            
            /// <summary>
            /// Duration of displaying this log, if not set it will be automatically calculated
            /// </summary>
            public float CustomDisplayDuration { get; set; } = 0.0f;
        }

        /// <summary>
        /// Number of words required to display log for a minute
        /// </summary>
        private const int WordsPerMinute = 233;

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// UI Text object where the text will be set
        /// </summary>
        [SerializeField]
        private Text uiText;
#pragma warning restore 0649

        /// <summary>
        /// Is the log panel currently visible
        /// </summary>
        private bool isVisible;

        /// <summary>
        /// Queue of all logs to be viewed
        /// </summary>
        private readonly Queue<LogData> logsQueue = new Queue<LogData>();

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected void OnEnable()
        {
            //Disable popup if it was shown without setup
            if (!isVisible)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Enqueues log data to be displayed on the panel
        /// </summary>
        /// <param name="logData">Log with settings that will be displayed</param>
        public void EnqueueLog(LogData logData)
        {
            logsQueue.Enqueue(logData);
            Show();
        }

        /// <summary>
        /// Enqueues log error to be displayed on the panel
        /// </summary>
        /// <param name="text">Error text that will be displayed</param>
        public void EnqueueInfo(string text)
        {
            var logData = new LogData()
            {
                Type = LogType.Info,
                Text = text,
                TextColor = Color.white
            };
            logsQueue.Enqueue(logData);
            Show();
        }
        
        /// <summary>
        /// Enqueues log error to be displayed on the panel
        /// </summary>
        /// <param name="text">Error text that will be displayed</param>
        public void EnqueueWarning(string text)
        {
            var logData = new LogData()
            {
                Type = LogType.Warning,
                Text = text,
                TextColor = Color.yellow
            };
            logsQueue.Enqueue(logData);
            Show();
        }
        
        /// <summary>
        /// Enqueues log error to be displayed on the panel
        /// </summary>
        /// <param name="text">Error text that will be displayed</param>
        public void EnqueueError(string text)
        {
            var logData = new LogData()
            {
                Type = LogType.Error,
                Text = text,
                TextColor = Color.red
            };
            logsQueue.Enqueue(logData);
            Show();
        }

        /// <summary>
        /// Shows the log panel
        /// </summary>
        private void Show()
        {
            if (isVisible)
                return;
            isVisible = true;
            gameObject.SetActive(true);
            StartCoroutine(LogCoroutine());
        }

        /// <summary>
        /// Hides the log panel
        /// </summary>
        private void Hide()
        {
            if (!isVisible)
                return;
            isVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Calculates the duration of displaying text 
        /// </summary>
        /// <param name="text">Text that will be displayed</param>
        /// <returns>Duration of displaying the text in seconds</returns>
        private static float CalculateTextDuration(string text)
        {
            var delimiters = new[] {' ', '\r', '\n'};
            var words = text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
            return 1.0f+ 60.0f * words / WordsPerMinute;
        }

        /// <summary>
        /// Coroutine that displays logs one by one
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator LogCoroutine()
        {
            var log = logsQueue.Dequeue();
            while (log != null)
            {
                uiText.text = log.Text;
                uiText.color = log.TextColor;
                var displayDuration = log.CustomDisplayDuration > 0.0f
                    ? log.CustomDisplayDuration
                    : CalculateTextDuration(uiText.text);
                switch (log.Type)
                {
                    case LogType.Info:
                        Debug.Log(log.Text);
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(log.Text);
                        break;
                    case LogType.Error:
                        Debug.LogError(log.Text);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                yield return new WaitForSecondsRealtime(displayDuration);
                log = logsQueue.Count == 0 ? null : logsQueue.Dequeue();
            }

            Hide();
        }
    }
}