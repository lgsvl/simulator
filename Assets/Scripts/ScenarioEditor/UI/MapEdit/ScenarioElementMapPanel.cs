/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapEdit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Elements;
    using Managers;
    using Simulator.Utilities;
    using UnityEngine;

    /// <summary>
    /// Panel allowing for a quick edit of a map element
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScenarioElementMapPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Screen position offset from the selected element
        /// </summary>
        [SerializeField]
        private Vector2 offsetFromElement;

        /// <summary>
        /// Rendering camera used for rendering the scenario elements
        /// </summary>
        [SerializeField]
        private Camera renderingCamera;

        /// <summary>
        /// Prefab of a single agent edit button
        /// </summary>
        [SerializeField]
        private GameObject agentEditButtonPrefab;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Cached currently selected element
        /// </summary>
        private ScenarioElement element;

        /// <summary>
        /// Available element map edit features
        /// </summary>
        private IElementMapEdit[] agentEdits;

        /// <summary>
        /// Available buttons for all the edit features
        /// </summary>
        private readonly List<ElementEditButton> buttons = new List<ElementEditButton>();

        /// <summary>
        /// Unity Start method
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            Deinitialize();
        }

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        private void OnEnable()
        {
            Initialize();
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += SelectedOtherElement;
            var interfaceType = typeof(IElementMapEdit);
            var agentEditType =
                ReflectionCache.FindTypes((type) => !type.IsAbstract && interfaceType.IsAssignableFrom(type));
            agentEdits = new IElementMapEdit[agentEditType.Count];
            for (var i = 0; i < agentEditType.Count; i++)
            {
                var buttonType = agentEditType[i];
                agentEdits[i] = Activator.CreateInstance(buttonType) as IElementMapEdit;
            }

            isInitialized = true;
            SelectedOtherElement(ScenarioManager.Instance.SelectedElement);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        private void Deinitialize()
        {
            if (!isInitialized)
                return;
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= SelectedOtherElement;
            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void SelectedOtherElement(ScenarioElement selectedElement)
        {
            element = selectedElement;
            if (element == null)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected element
        /// </summary>
        private void Show()
        {
            PrepareButtonsForElement();
            gameObject.SetActive(true);
            StartCoroutine(FollowElement());
        }

        /// <summary>
        /// Prepares edit buttons for currently selected element
        /// </summary>
        private void PrepareButtonsForElement()
        {
            var buttonId = 0;
            foreach (var agentEdit in agentEdits)
            {
                if (!agentEdit.CanEditElement(element)) continue;
                if (buttonId >= buttons.Count)
                {
                    var newButton = Instantiate(agentEditButtonPrefab, transform);
                    buttons.Add(newButton.GetComponent<ElementEditButton>());
                }

                buttons[buttonId].gameObject.SetActive(true);
                buttons[buttonId++].Initialize(agentEdit);
                agentEdit.CurrentElement = element;
            }

            //Disable unused buttons
            for (var i = buttonId; i < buttons.Count; i++)
                buttons[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Coroutine which follows selected element position on the screen
        /// </summary>
        /// <returns>IEnumerator</returns>
        /// <exception cref="ArgumentException">Panel requires RectTransform component</exception>
        private IEnumerator FollowElement()
        {
            var rectPosition = transform as RectTransform;
            if (rectPosition == null)
                throw new ArgumentException($"{GetType().Name} requires {nameof(RectTransform)} component.");
            while (gameObject.activeSelf)
            {
                Vector2 newPosition = renderingCamera.WorldToScreenPoint(element.transform.position);
                newPosition += offsetFromElement;
                rectPosition.anchoredPosition = newPosition;
                yield return null;
            }
        }

        /// <summary>
        /// Hides the quick edit panel
        /// </summary>
        private void Hide()
        {
            gameObject.SetActive(false);
            element = null;
        }
    }
}