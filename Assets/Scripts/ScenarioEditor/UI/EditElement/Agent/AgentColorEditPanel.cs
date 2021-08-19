/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Agent
{
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Managers;
    using ScenarioEditor.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Edit panel for the agent's color
    /// </summary>
    public class AgentColorEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Image that represents color of the agent
        /// </summary>
        [SerializeField]
        private Image colorImage;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;
        
        /// <summary>
        /// Color extension that is edited by this panel
        /// </summary>
        private AgentColorExtension colorExtension;

        /// <summary>
        /// Currently edited scenario agent reference
        /// </summary>
        private ScenarioAgent selectedAgent;

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
            if (colorExtension != null)
                colorExtension.ColorChanged -= SelectedAgentOnColorChanged;

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                colorExtension = selectedAgent.GetExtension<AgentColorExtension>();
                if (colorExtension == null)
                    Hide();
                else
                {
                    colorExtension.ColorChanged += SelectedAgentOnColorChanged;
                    Show();
                }
            }
            else
            {
                Hide();
            }
        }


        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            colorImage.color = colorExtension.AgentColor;
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Hides the panel and clears current agent
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Method invoked when selected agent changes the color
        /// </summary>
        /// <param name="newColor">Agent new color</param>
        private void SelectedAgentOnColorChanged(Color newColor)
        {
            if (colorImage != null)
                colorImage.color = newColor;
        }

        /// <summary>
        /// Shows the color picker to change the selected agent color
        /// </summary>
        public void EditColor()
        {
            var colorPicker = ScenarioManager.Instance.colorPicker;
            var previousColor = colorExtension.AgentColor;
            colorPicker.Show(colorExtension.AgentColor,
                color => colorExtension.AgentColor = color,
                () => ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoChangeColor(colorExtension, previousColor)));
        }
    }
}