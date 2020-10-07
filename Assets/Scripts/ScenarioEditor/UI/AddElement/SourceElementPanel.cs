/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
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
        /// Main text of this panel
        /// </summary>
        [SerializeField]
        private Text text;
#pragma warning restore 0649

        /// <summary>
        /// Cached scenario element source class which is used for adding new elements from this panel
        /// </summary>
        private ScenarioElementSource source;
        
        /// <summary>
        /// Cached scenario element source variant handled by this panel
        /// </summary>
        private SourceVariant variant;

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="source">Scenario element source class which will be used for adding new elements from this panel</param>
        /// <param name="variant">Cached scenario element source variant handled by this panel</param>
        public void Initialize(ScenarioElementSource source, SourceVariant variant)
        {
            this.source = source;
            this.variant = variant;
            if (!variant.IsPrepared)
            {
                text.text = $"{UnpreparedSign} {variant.Name} {UnpreparedSign}";
                variant.Prepared += VariantOnPrepared;
            }
            else text.text = variant.Name;
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        public void OnDestroy()
        {
            variant.Prepared -= VariantOnPrepared;
        }

        /// <summary>
        /// Method invoked when bound variant becomes prepared
        /// </summary>
        private void VariantOnPrepared()
        {
            text.text = variant.Name;
            variant.Prepared -= VariantOnPrepared;
        }

        /// <summary>
        /// Method invokes when this source is selected in the UI
        /// </summary>
        public void OnElementSelected()
        {
            if (variant.IsBusy)
            {
                ScenarioManager.Instance.logPanel.EnqueueInfo($"Downloading of the {variant.Name} agent is currently in progress.");
                return;
            }

            if (!variant.IsPrepared)
                variant.Prepare();
            else
                source.OnVariantSelected(variant);
        }
    }
}