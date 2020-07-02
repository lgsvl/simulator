/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    public class TriggerEffectorPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// UI Text with the effector name
        /// </summary>
        [SerializeField]
        private Text title;

        /// <summary>
        /// UI InputField for the effector value
        /// </summary>
        [SerializeField]
        private InputField valueInputField;
        
        /// <summary>
        /// Properties object that can be expanded and hidden
        /// </summary>
        [SerializeField]
        private GameObject expandableProperties;
#pragma warning restore 0649
        
        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;
        
        /// <summary>
        /// Trigger effector type linked to this panel
        /// </summary>
        private TriggerEffector triggerEffector;
        
        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="triggerPanel">Parent trigger panel</param>
        /// <param name="effector">Trigger effector type linked to this panel</param>
        public void Initialize(TriggerEditPanel triggerPanel, TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            triggerEffector = effector;
            title.text = triggerEffector.TypeName;
            expandableProperties.SetActive(true);
            valueInputField.text = effector.Value.ToString("F");
        }

        /// <summary>
        /// Removes linked effector from the trigger and returns it to the pool
        /// </summary>
        public void Remove()
        {
            parentPanel.RemoveEffector(triggerEffector);
        }

        /// <summary>
        /// Toggles the expandable object showing or hiding expandable properties
        /// </summary>
        public void ToggleExpandableObject()
        {
            expandableProperties.SetActive(!expandableProperties.activeSelf);
            UIUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Sets the trigger effector value
        /// </summary>
        /// <param name="valueString">Value that should be set to the effector</param>
        public void SetValue(string valueString)
        {
            if (float.TryParse(valueString, out var value))
                SetValue(value);
        }

        /// <summary>
        /// Sets the trigger effector value
        /// </summary>
        /// <param name="value">Value that should be set to the effector</param>
        public void SetValue(float value)
        {
            ScenarioManager.Instance.IsScenarioDirty = true;
            triggerEffector.Value = value;
        }
    }
}