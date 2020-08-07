/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Playback
{
    using UnityEngine;

    /// <summary>
    /// Scenario playback panel that controls the playback speed
    /// </summary>
    public class SpeedControlPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Parent playback panel
        /// </summary>
        [SerializeField]
        private PlaybackPanel playbackPanel;
        
        /// <summary>
        /// Playback speed button which is selected as default
        /// </summary>
        [SerializeField]
        private PlaybackSpeedButton selectedButton;
#pragma warning restore 0649
        
        /// <summary>
        /// Unity Start method
        /// </summary>
        public void Start()
        {
            SpeedSelected(selectedButton);
        }

        /// <summary>
        /// Method which changes playback speed according to currently selected button
        /// </summary>
        /// <param name="speedButton">Playback speed button that is selected</param>
        public void SpeedSelected(PlaybackSpeedButton speedButton)
        {
            if (selectedButton!=null)
                selectedButton.Unmark();
            selectedButton = speedButton;
            playbackPanel.SetPlaybackSpeed(selectedButton.speedValue);
            selectedButton.MarkAsCurrent();
        }
    }
}