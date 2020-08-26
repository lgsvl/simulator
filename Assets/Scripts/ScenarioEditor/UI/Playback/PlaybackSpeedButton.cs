/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.Playback
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Button object which includes data required to change the playback speed
    /// </summary>
    public class PlaybackSpeedButton : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Text that views speed bound to this button
        /// </summary>
        [SerializeField]
        private Text speedText;
#pragma warning restore 0649

        /// <summary>
        /// Default color of the speed text
        /// </summary>
        private Color defaultColor;

        /// <summary>
        /// Speed value applied when this button is selected
        /// </summary>
        public float speedValue;

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            defaultColor = speedText.color;
        }

        /// <summary>
        /// Marks this button as currently selected
        /// </summary>
        public void MarkAsCurrent()
        {
            speedText.color = new Color(0.9294118f, 0.2196078f, 0.4f);
        }

        /// <summary>
        /// Unmarks this button as it is no longer selected
        /// </summary>
        public void Unmark()
        {
            speedText.color = defaultColor;
        }
    }
}