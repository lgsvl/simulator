/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Playback
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Elements;
    using Input;
    using Inspector;
    using Managers;
    using ScenarioEditor.Playback;
    using Simulator.Utilities;
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
            /// Scenario is currently precaching the playback
            /// </summary>
            Precaching = 1,

            /// <summary>
            /// Scenario is currently being playing
            /// </summary>
            Playing = 2,

            /// <summary>
            /// Scenario is currently paused
            /// </summary>
            Paused = 3
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Game object that includes all the playback control elements
        /// </summary>
        [SerializeField]
        private GameObject controlPanel;

        /// <summary>
        /// Game object that is displayed instead of control panel when the playback is being precached
        /// </summary>
        [SerializeField]
        private GameObject precachingPanel;
        
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
        /// Speed of the playback precache
        /// </summary>
        private const float PrecachePlaybackSpeed = 1.0f;

        /// <summary>
        /// Playback controllers that handle scenario elements during the play mode
        /// </summary>
        private List<PlaybackController> controllers = new List<PlaybackController>();

        /// <summary>
        /// Cached element reference that was selected before entering the playback mode
        /// </summary>
        private ScenarioElement selectedElement;

        /// <summary>
        /// Coroutine that handles current playback
        /// </summary>
        private IEnumerator playCoroutine;

        /// <summary>
        /// Coroutine that handles precaching playback
        /// </summary>
        private IEnumerator precachePlaybackCoroutine;

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
            var controllerTypes = ReflectionCache.FindTypes(type => type.IsSubclassOf(typeof(PlaybackController)));
            foreach (var controllerType in controllerTypes)
            {
                if (Activator.CreateInstance(controllerType) is PlaybackController controller)
                {
                    controllers.Add(controller);
                    controller.Initialize();
                }
            }

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
            precachePlaybackCoroutine = PrecachePlayback();
            State = PlaybackState.Precaching;
            controlPanel.SetActive(false);
            precachingPanel.SetActive(true);
            gameObject.SetActive(true);
            StartCoroutine(precachePlaybackCoroutine);
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            // If hide occurs while playback is precached, stop all coroutines as nested coroutines may be attached
            if (precachePlaybackCoroutine != null)
            {
                StopCoroutine(precachePlaybackCoroutine);
                StopAllCoroutines();
                precachePlaybackCoroutine = null;
            }

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
        /// Returns the playback controller of given type, null if not available
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="PlaybackController"/> that will be returned</typeparam>
        /// <returns>Playback controller of given type, null if not available</returns>
        public T GetController<T>() where T : PlaybackController
        {
            foreach (var controller in controllers)
            {
                if (controller is T controllerT)
                    return controllerT;
            }

            return null;
        }

        /// <summary>
        /// Precache playback data
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator PrecachePlayback()
        {
            Time.timeScale = PrecachePlaybackSpeed;
            timeSlider.interactable = false;
            durationLabel.text = "?s";

            // Start precaching playback on all the controllers
            var coroutines = new List<Coroutine>();
            for (var i = 0; i < controllers.Count; i++)
            {
                coroutines.Add(StartCoroutine(controllers[i].PrecachePlayback(this)));
            }

            // Wait for all the coroutines
            for (var i = 0; i < coroutines.Count; i++)
            {
                yield return coroutines[i];
            }

            // Find the longest playback duration
            for (var i = 0; i < controllers.Count; i++)
            {
                if (controllers[i].Duration > duration)
                    duration = controllers[i].Duration;
            }

            timeSlider.interactable = true;
            durationLabel.text = $"{duration:F1}s";
            progress = 0.0f;
            Time.timeScale = playbackSpeed;
            controlPanel.SetActive(true);
            precachingPanel.SetActive(false);
            State = PlaybackState.Idle;
            UpdatePlayback();
        }

        /// <summary>
        /// Start the scenario playback
        /// </summary>
        public void Play()
        {
            if (State == PlaybackState.Playing || State == PlaybackState.Precaching)
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
            if (State == PlaybackState.Precaching)
                return;

            if (State == PlaybackState.Idle)
            {
                progress = 0.0f;
                UpdatePlayback();
                return;
            }

            State = PlaybackState.Idle;
            progress = 0.0f;
            UpdatePlayback();
            Time.timeScale = 0.0f;
            if (playCoroutine != null)
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
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator PlayCoroutine()
        {
            if (progress >= 1.0f)
                progress = 0.0f;
            var endFrame = new WaitForEndOfFrame();
            var previousProgress = -1.0f;
            while (progress < 1.0f)
            {
                if (!Mathf.Approximately(progress, previousProgress))
                    UpdatePlayback();
                yield return endFrame;
                previousProgress = progress;
                progress += Time.deltaTime / duration;
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