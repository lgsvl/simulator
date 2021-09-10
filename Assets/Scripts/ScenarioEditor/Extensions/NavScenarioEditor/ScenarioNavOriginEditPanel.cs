/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Nav
{
    using System.Globalization;
    using Elements;
    using Managers;
    using UI.EditElement.Effectors;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    /// <summary>
    /// Edit panel for changing the <see cref="ScenarioNavOrigin"/> element
    /// </summary>
    public class ScenarioNavOriginEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Input field for the origin X
        /// </summary>
        [SerializeField]
        private InputField originXInput;
        
        /// <summary>
        /// Input field for the origin Y
        /// </summary>
        [SerializeField]
        private InputField originYInput;
        
        /// <summary>
        /// Input field for the rotation
        /// </summary>
        [SerializeField]
        private InputField rotationInput;
#pragma warning restore 0649
        
        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Currently edited scenario <see cref="ScenarioNavOrigin"/> reference
        /// </summary>
        private ScenarioNavOrigin selectedNavOrigin;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            isInitialized = true;
            OnSelectedOtherElement(null, ScenarioManager.Instance.SelectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="previousElement">Scenario element that has been deselected</param>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement selectedElement)
        {
            //Detach from current agent events
            if (selectedNavOrigin != null) { }

            selectedNavOrigin = selectedElement as ScenarioNavOrigin;
            //Attach to selected <see cref="ScenarioNavOrigin"/>
            if (selectedNavOrigin != null)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected <see cref="ScenarioNavOrigin"/>
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
            originXInput.text = selectedNavOrigin.NavOrigin.OriginX.ToString(CultureInfo.CurrentCulture);
            originYInput.text = selectedNavOrigin.NavOrigin.OriginY.ToString(CultureInfo.CurrentCulture);
            rotationInput.text = selectedNavOrigin.NavOrigin.Rotation.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Hides the panel and clears current <see cref="ScenarioNavOrigin"/>
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Method invoked when origin X input changes
        /// </summary>
        /// <param name="offsetX">Offset X value in string</param>
        public void OnOriginXInputChanged(string offsetX)
        {
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                originXInput, selectedNavOrigin.NavOrigin.OriginX.ToString("F"), OnOriginXChanged));
            OnOriginXChanged(offsetX);
        }

        /// <summary>
        /// Method invoked when origin X changes
        /// </summary>
        /// <param name="offsetX">Offset X value in string</param>
        public void OnOriginXChanged(string offsetX)
        {
            OnOriginXChanged(float.Parse(offsetX));
        }

        /// <summary>
        /// Method invoked when origin X changes
        /// </summary>
        /// <param name="originX">Origin X value</param>
        public void OnOriginXChanged(float originX)
        {
            selectedNavOrigin.NavOrigin.OriginX = originX;
        }

        /// <summary>
        /// Method invoked when offset Y input changes
        /// </summary>
        /// <param name="offsetY">Offset Y value in string</param>
        public void OnOriginYInputChanged(string offsetY)
        {
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                originYInput, selectedNavOrigin.NavOrigin.OriginY.ToString("F"), OnOriginYChanged));
            OnOriginYChanged(offsetY);
        }

        /// <summary>
        /// Method invoked when offset Y changes
        /// </summary>
        /// <param name="offsetY">Offset Y value in string</param>
        public void OnOriginYChanged(string offsetY)
        {
            OnOriginYChanged(float.Parse(offsetY));
        }

        /// <summary>
        /// Method invoked when origin Y changes
        /// </summary>
        /// <param name="originY">Origin Y value</param>
        public void OnOriginYChanged(float originY)
        {
            selectedNavOrigin.NavOrigin.OriginY = originY;
        }

        /// <summary>
        /// Method invoked when rotation input changes
        /// </summary>
        /// <param name="rotation">Rotation value in string</param>
        public void OnRotationInputChanged(string rotation)
        {
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                rotationInput, selectedNavOrigin.NavOrigin.Rotation.ToString("F"), OnRotationChanged));
            OnRotationChanged(rotation);
        }

        /// <summary>
        /// Method invoked when rotation changes
        /// </summary>
        /// <param name="rotation">Rotation value in string</param>
        public void OnRotationChanged(string rotation)
        {
            var floatRotation = float.Parse(rotation) % 360.0f;
            OnRotationChanged(floatRotation);
        }

        /// <summary>
        /// Method invoked when rotation changes
        /// </summary>
        /// <param name="rotation">Rotation value</param>
        public void OnRotationChanged(float rotation)
        {
            selectedNavOrigin.NavOrigin.Rotation = rotation;
            rotationInput.SetTextWithoutNotify(rotation.ToString("F"));
        }
    }
}