/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Playback
{
    using System.Collections;
    using System.Collections.Generic;
    using Elements;
    using Input;
    using Inspector;
    using Managers;
    using ScenarioEditor.Playback;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI panel which allows playing a scenario inside the VSE
    /// </summary>
    public class PlaybackPanel : InspectorContentPanel
    {
        /// <summary>
        /// State type of scenario playback
        /// </summary>
        public enum PlaybackState
        {
            /// <summary>
            /// Scenario is currently not being played
            /// </summary>
            Idle = 0,

            /// <summary>
            /// Scenario is currently being playing
            /// </summary>
            Playing = 1,

            /// <summary>
            /// Scenario is currently paused
            /// </summary>
            Paused = 2
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Playback controllers that handle scenario elements during the play mode
        /// </summary>
        [SerializeField]
        private List<PlaybackController> controllers = new List<PlaybackController>();
        
        /// <summary>
        /// Reference to the slider that handles the playback time
        /// </summary>
        [SerializeField]
        private Slider timeSlider;

        /// <summary>
        /// Text that displays the duration of the scenario playback
        /// </summary>
        [SerializeField]
        private Text durationLabel;

        /// <summary>
        /// Text that displays current time of the scenario playback
        /// </summary>
        [SerializeField]
        private Text currentTimeLabel;
#pragma warning restore 0649

        /// <summary>
        /// Cached element reference that was selected before entering the playback mode
        /// </summary>
        private ScenarioElement selectedElement;

        /// <summary>
        /// Coroutine that handles current playback
        /// </summary>
        private IEnumerator playCoroutine;

        /// <summary>
        /// Did the playback panel lock input manager semaphore
        /// </summary>
        private bool didLockSemaphore;

        /// <summary>
        /// Duration of the current scenario playback
        /// </summary>
        private float duration;

        /// <summary>
        /// Progress of the current scenario playback
        /// </summary>
        private float progress;

        /// <summary>
        /// Time scale of the playback
        /// </summary>
        private float playbackSpeed;
        
        /// <summary>
        /// Current state of the playback
        /// </summary>
        public PlaybackState State { get; private set; }
        
        /// <inheritdoc/>
        public override void Initialize()
        {
            timeSlider.SetValueWithoutNotify(0.0f);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
        }

        /// <inheritdoc/>
        public override void Show()
        {
            selectedElement = ScenarioManager.Instance.SelectedElement;
            ScenarioManager.Instance.SelectedElement = null;
            ScenarioManager.Instance.GetExtension<InputManager>().ElementSelectingSemaphore.Lock();
            didLockSemaphore = true;
            duration = 0.0f;
            for (var i = 0; i < controllers.Count; i++)
            {
                controllers[i].Initialize();
                if (controllers[i].Duration > duration)
                    duration = controllers[i].Duration;
            }

            durationLabel.text = $"{duration:F1}s";
            progress = 0.0f;
            UpdatePlayback();
            gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            Stop();
            for (var i = 0; i < controllers.Count; i++)
                controllers[i].Deinitialize();
            gameObject.SetActive(false);
            if (didLockSemaphore)
                ScenarioManager.Instance.GetExtension<InputManager>().ElementSelectingSemaphore.Unlock();
            didLockSemaphore = false;
            ScenarioManager.Instance.SelectedElement = selectedElement;
            selectedElement = null;
        }

        /// <summary>
        /// Start the scenario playback
        /// </summary>
        public void Play()
        {
            if (State == PlaybackState.Playing)
                return;
            State = PlaybackState.Playing;
            Time.timeScale = playbackSpeed;
            if (playCoroutine != null)
                return;
            playCoroutine = PlayCoroutine();
            StartCoroutine(playCoroutine);
        }

        /// <summary>
        /// Stop the scenario playback
        /// </summary>
        public void Stop()
        {
            if (State == PlaybackState.Idle)
            {
                progress = 0.0f;
                UpdatePlayback();
                return;
            }

            for (var i = 0; i < controllers.Count; i++)
                controllers[i].Reset();
            State = PlaybackState.Idle;
            progress = 0.0f;
            UpdatePlayback();
            Time.timeScale = 0.0f;
            if (playCoroutine!=null)
                StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        /// <summary>
        /// Pauses the scenario playback
        /// </summary>
        public void Pause()
        {
            if (State != PlaybackState.Playing)
                return;
            State = PlaybackState.Paused;
            Time.timeScale = 0.0f;
        }

        /// <summary>
        /// Coroutine that handles current playback
        /// </summary>
        /// <returns>Coroutine</returns>
        private IEnumerator PlayCoroutine()
        {
            if (progress>=1.0f)
                progress = 0.0f;
            var endFrame = new WaitForEndOfFrame();
            var previousProgress = -1.0f;
            while (progress < 1.0f)
            {
                if (!Mathf.Approximately(progress, previousProgress))
                    UpdatePlayback();
                yield return endFrame;
                previousProgress = progress;
                progress += Time.deltaTime/duration;
            }
            playCoroutine = null;
            progress = 1.0f;
            UpdatePlayback();
            State = PlaybackState.Idle;
        }

        /// <summary>
        /// Updates the playback elements and controllers
        /// </summary>
        private void UpdatePlayback()
        {
            var time = progress * duration;
            for (var i = 0; i < controllers.Count; i++)
                controllers[i].PlaybackUpdate(time);
            timeSlider.SetValueWithoutNotify(progress);
            currentTimeLabel.text = $"{time:F1}s";
            var rectTransform = currentTimeLabel.rectTransform;
            var position = rectTransform.position;
            position.x = timeSlider.handleRect.position.x;
            rectTransform.position = position;
        }

        /// <summary>
        /// Forces the progress change of current playback
        /// </summary>
        /// <param name="newProgress">Progress that will be set</param>
        public void ForceProgress(float newProgress)
        {
            progress = Mathf.Clamp(newProgress, 0.0f, 1.0f);
            UpdatePlayback();
        }

        /// <summary>
        /// Sets the playback speed
        /// </summary>
        /// <param name="speed">Time scale of the playback</param>
        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = speed;
            if (State == PlaybackState.Playing)
                Time.timeScale = playbackSpeed;
        }
    }
}