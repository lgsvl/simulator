/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using System;
    using System.Collections;
    using Agents;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Scenario element source panel visualize a scenario element source for adding new elements
    /// </summary>
    public class SourceElementPanel : MonoBehaviour
    {
        /// <summary>
        /// Sign that is added to the name text when bound variant is unprepared
        /// </summary>
        private static string UnpreparedSign = "";

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Content of this panel which is disabled if there is no valid variant set
        /// </summary>
        [SerializeField]
        private GameObject content;

        /// <summary>
        /// Main text of this panel
        /// </summary>
        [SerializeField]
        private Text text;
#pragma warning restore 0649

        /// <summary>
        /// Add elements panel that handles this element panel
        /// </summary>
        private AddElementsPanel addElementsPanel;

        /// <summary>
        /// Cached scenario element source class which is used for adding new elements from this panel
        /// </summary>
        private ScenarioElementSource source;

        /// <summary>
        /// Cached scenario element source variant handled by this panel
        /// </summary>
        private SourceVariant variant;

        /// <summary>
        /// Coroutine invoked to show the panel with variant description
        /// </summary>
        private IEnumerator showDescriptionCoroutine;

        /// <summary>
        /// Cached scenario element source variant handled by this panel
        /// </summary>
        public SourceVariant Variant => variant;

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="sourcePanel">Parent source panel</param>
        /// <param name="source">Scenario element source class which will be used for adding new elements from this panel</param>
        /// <param name="variant">Cached scenario element source variant handled by this panel</param>
        public void Initialize(SourcePanel sourcePanel, ScenarioElementSource source, SourceVariant variant)
        {
            addElementsPanel = GetComponentInParent<AddElementsPanel>();
            this.source = source;
            this.variant = variant;
            if (variant == null)
            {
                (sourcePanel.MultiplePages ? content : gameObject).SetActive(false);
            }
            else
            {
                (sourcePanel.MultiplePages ? content : gameObject).SetActive(true);
                if (!variant.IsPrepared)
                {
                    var progress = variant.IsBusy
                        ? $"{variant.PreparationProgress:F1}% "
                        : "";
                    text.text = $"{progress}{UnpreparedSign} {variant.Name} {UnpreparedSign}";
                    variant.Prepared += VariantOnPrepared;
                }
                else text.text = variant.Name;
            }
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (Variant != null)
                Variant.Prepared -= VariantOnPrepared;
            source = null;
            variant = null;
        }

        /// <summary>
        /// Method invoked when bound variant becomes prepared
        /// </summary>
        private void VariantOnPrepared()
        {
            text.text = Variant.Name;
            Variant.Prepared -= VariantOnPrepared;
        }

        /// <summary>
        /// Method invokes when this source is selected in the UI
        /// </summary>
        public void OnElementSelected()
        {
            if (Variant.IsBusy)
            {
                ScenarioManager.Instance.logPanel.EnqueueInfo(
                    $"Downloading of the {Variant.Name} agent is currently in progress.");
                return;
            }

            if (!Variant.IsPrepared)
            {
                var progress = new Progress<SourceVariant>(downloadedVariant =>
                {
                    if (text != null && Variant == downloadedVariant)
                        text.text = $"{downloadedVariant.PreparationProgress:F1}% {UnpreparedSign} {variant.Name} {UnpreparedSign}";
                });
                Variant.Prepare(progress);
            }
            else
                source.OnVariantSelected(Variant);
        }

        /// <summary>
        /// Method invokes when the pointer enters this panel collider
        /// </summary>
        public void OnPointerEnter()
        {
            if (showDescriptionCoroutine != null)
                return;
            showDescriptionCoroutine = DelayedDisplayDescription();
            StartCoroutine(showDescriptionCoroutine);
        }

        /// <summary>
        /// Coroutine that invokes displaying the bound variant description after a short delay
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator DelayedDisplayDescription()
        {
            yield return new WaitForSecondsRealtime(addElementsPanel.DescriptionPanel.ShowDelay);
            addElementsPanel.DescriptionPanel.Show(transform as RectTransform, variant);
            showDescriptionCoroutine = null;
        }

        /// <summary>
        /// Method invokes when the pointer exits this panel collider
        /// </summary>
        public void OnPointerExit()
        {
            addElementsPanel.DescriptionPanel.Hide(variant);
            if (showDescriptionCoroutine == null) return;
            StopCoroutine(showDescriptionCoroutine);
            showDescriptionCoroutine = null;
        }
    }
}