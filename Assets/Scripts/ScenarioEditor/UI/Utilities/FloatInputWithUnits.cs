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
    using System.Globalization;
    using System.Linq;
    using Elements;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Input field with float values connected to a dropdown to select different unit types
    /// </summary>
    public class FloatInputWithUnits : MonoBehaviour
    {
        /// <summary>
        /// Defined unit type for this input field
        /// </summary>
        [Serializable]
        public class UnitType
        {
            /// <summary>
            /// Full name of this unit type
            /// </summary>
            public string fullName;

            /// <summary>
            /// Short name of this unit type
            /// </summary>
            public string shortName;

            /// <summary>
            /// Factor used to calculate to the base
            /// </summary>
            public float factorToBase = 1.0f;
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Defined unit types for this input field
        /// </summary>
        [SerializeField]
        private List<UnitType> unitTypes;

        /// <summary>
        /// Input field for editing value
        /// </summary>
        [SerializeField]
        private InputField input;

        /// <summary>
        /// Dropdown for changing the value unit
        /// </summary>
        [SerializeField]
        private Dropdown unitDropdown;
#pragma warning restore 0649

        /// <summary>
        /// Current value of this input
        /// </summary>
        private float currentValue;

        /// <summary>
        /// Currently selected unit type index
        /// </summary>
        private int currentUnit;

        /// <summary>
        /// Player prefs key used to same the current unit type
        /// </summary>
        private string playerPrefsKey;

        /// <summary>
        /// Callback invoked then the value is changed by the input field
        /// </summary>
        private Action<ScenarioElement, float> valueApply;

        /// <summary>
        /// Input field for editing value
        /// </summary>
        public InputField UnityInputField => input;
        
        /// <summary>
        /// Current scenario element which indicates the edit input context
        /// </summary>
        public ScenarioElement CurrentContext { get; set; }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            UnityInputField.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="unitTypePrefsKey">Player prefs key used to same the current unit type</param>
        /// <param name="valueApply">Callback invoked then the value is changed by the input field</param>
        /// <param name="currentContext">Current scenario element which indicates the edit input context</param>
        public void Initialize(string unitTypePrefsKey, Action<ScenarioElement, float> valueApply,
            ScenarioElement currentContext)
        {
            playerPrefsKey = unitTypePrefsKey;
            this.valueApply = valueApply;
            CurrentContext = currentContext;

            currentUnit = PlayerPrefs.GetInt(unitTypePrefsKey, 0);
            if (currentUnit >= unitTypes.Count)
                currentUnit = 0;
            unitDropdown.options.Clear();
            unitDropdown.AddOptions(unitTypes.Select(unit => unit.shortName).ToList());
            unitDropdown.SetValueWithoutNotify(currentUnit);
            UnitDropdownChanged(currentUnit);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            playerPrefsKey = null;
            valueApply = null;
            currentValue = 0;
        }

        /// <summary>
        /// Method changing the variant of the currently selected scenario agent
        /// </summary>
        /// <param name="unitId">Speed unit type id</param>
        public void UnitDropdownChanged(int unitId)
        {
            currentUnit = unitId;
            PlayerPrefs.SetInt(playerPrefsKey, unitId);
            UnityInputField.text = ConvertFromBase(currentValue, currentUnit).ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts the source unit type value to base unit type
        /// </summary>
        /// <param name="value">Value in the source unit type</param>
        /// <param name="sourceUnitIndex">Source speed unit type index</param>
        /// <returns>Converted value to base type</returns>
        private float ConvertToBase(float value, int sourceUnitIndex)
        {
            return value * unitTypes[sourceUnitIndex].factorToBase;
        }

        /// <summary>
        /// Converts the base unit value to target unit type
        /// </summary>
        /// <param name="baseValue">Value in base unit</param>
        /// <param name="targetUnitIndex">Target unit type index</param>
        /// <returns>Converted speed to target unit type</returns>
        private float ConvertFromBase(float baseValue, int targetUnitIndex)
        {
            return baseValue / unitTypes[targetUnitIndex].factorToBase;
        }

        /// <summary>
        /// Changes the value according to currently selected unit type and current text
        /// </summary>
        public void OnValueInputApply()
        {
            OnValueInputApply(input.text);
        }

        /// <summary>
        /// Changes the value according to currently selected unit type
        /// </summary>
        /// <param name="valueString">Value in string</param>
        public void OnValueInputApply(string valueString)
        {
            if (!float.TryParse(valueString, out var value)) return;

            var previousValue = currentValue;
            if (InputChangedValue(ConvertToBase(value, currentUnit)))
            {
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoFloatInputWithUnits(this, previousValue, CurrentContext));
            }
        }

        /// <summary>
        /// Method invoked then the input field changes the value
        /// </summary>
        /// <param name="baseValue">New value in the base unit type</param>
        /// <returns>True if value has been changed, false otherwise</returns>
        private bool InputChangedValue(float baseValue)
        {
            if (Mathf.Approximately(currentValue, baseValue))
                return false;
            currentValue = baseValue;
            valueApply?.Invoke(CurrentContext, currentValue);
            return true;
        }

        /// <summary>
        /// Changes the value of this input field without calling the callback
        /// </summary>
        /// <param name="baseValue">New value in the base unit type</param>
        /// <param name="context">Scenario element which indicates the edit input context</param>
        /// <param name="invokeApply">Should this method call invoke value apply callback</param>
        public void ExternalValueChange(float baseValue, ScenarioElement context, bool invokeApply)
        {
            if (Mathf.Approximately(currentValue, baseValue))
                return;
            currentValue = baseValue;
            UnityInputField.SetTextWithoutNotify(ConvertFromBase(currentValue, currentUnit)
                .ToString(CultureInfo.CurrentCulture));
            if (invokeApply)
                valueApply?.Invoke(context, currentValue);
        }
    }
}