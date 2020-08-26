/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit.Buttons
{
    using Elements;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Single feature that allows to edit map element
    /// </summary>
    public abstract class ElementMapEdit : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Text object displaying the edit feature title
        /// </summary>
        [SerializeField]
        protected Text titleText;
#pragma warning restore 0649
        
        /// <summary>
        /// Title of this feature
        /// </summary>
        public virtual string Title { get; }

        /// <summary>
        /// Currently edited element reference
        /// </summary>
        public ScenarioElement CurrentElement { get; set; }
        

        /// <summary>
        /// Initializes the button with the edit feature reference
        /// </summary>
        public virtual void Initialize()
        {
            titleText.text = Title;
        }

        /// <summary>
        /// Pressing the button invokes current corresponding edit feature
        /// </summary>
        public virtual void Pressed()
        {
            Edit();
        }

        /// <summary>
        /// Checks if this map edit panel can edit selected scenario element
        /// </summary>
        public abstract bool CanEditElement(ScenarioElement element);

        /// <summary>
        /// Method that starts editing current element with this feature
        /// </summary>
        public abstract void Edit();
    }
}