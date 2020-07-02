/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Button that represents a single element map edit feature
    /// </summary>
    public class ElementEditButton : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Text object displaying the edit feature title
        /// </summary>
        [SerializeField]
        private Text titleText;
#pragma warning restore 0649

        /// <summary>
        /// Corresponding edit feature reference that will be used when button is pressed
        /// </summary>
        private IElementMapEdit currentMapEditTarget;

        /// <summary>
        /// Initializes the button with the edit feature reference
        /// </summary>
        /// <param name="targetMapEdit">Corresponding edit feature reference that will be used when button is pressed</param>
        public void Initialize(IElementMapEdit targetMapEdit)
        {
            currentMapEditTarget = targetMapEdit;
            titleText.text = targetMapEdit.Title;
        }

        /// <summary>
        /// Pressing the button invokes current corresponding edit feature
        /// </summary>
        public void Pressed()
        {
            currentMapEditTarget.Edit();
        }
    }
}