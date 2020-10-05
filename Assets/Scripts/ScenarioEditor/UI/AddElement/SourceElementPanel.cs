/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.AddElement
{
    using System.Threading.Tasks;
    using Agents;
    using Elements;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Scenario element source panel visualize a scenario element source for adding new elements
    /// </summary>
    public class SourceElementPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Main image of this panel
        /// </summary>
        [SerializeField]
        private RawImage image;
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

            var nonBlockingTask = SetupTexture();
        }

        /// <summary>
        /// Setups the texture of this panel asynchronously waiting until the texture is ready
        /// </summary>
        /// <returns>Task</returns>
        private async Task SetupTexture()
        {
            while (variant.IconTexture == null)
                await Task.Delay(25);
            image.texture = variant.IconTexture;
        }

        /// <summary>
        /// Method invokes when this source is selected in the UI
        /// </summary>
        public void OnElementSelected()
        {
            source.OnVariantSelected(variant);
        }
    }
}