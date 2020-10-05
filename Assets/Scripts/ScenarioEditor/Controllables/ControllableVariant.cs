/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using Agents;
    using Controllable;
    using Managers;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Data describing a single controllable variant
    /// </summary>
    public class ControllableVariant : SourceVariant
    {
        /// <summary>
        /// Name of this controllable variant
        /// </summary>
        public string name;

        /// <summary>
        /// <see cref="IControllable"/> bound to this source variant
        /// </summary>
        public IControllable controllable;

        /// <summary>
        /// Texture used to visualize this agent variant in UI
        /// </summary>
        private Texture2D iconTexture;

        /// <inheritdoc/>
        public override string Name => name;

        /// <inheritdoc/>
        public override GameObject Prefab => controllable.gameObject;
        
        /// <inheritdoc/>
        public override Texture2D IconTexture
        {
            get
            {
                if (iconTexture == null)
                    iconTexture = ShotTexture();
                return iconTexture;
            }
        }

        /// <summary>
        /// Setup the controllable variant with the required data
        /// </summary>
        /// <param name="name">Name of this controllable variant</param>
        /// <param name="controllable"><see cref="IControllable"/> bound to this source variant</param>
        public void Setup(string name, IControllable controllable)
        {
            this.name = name;
            this.controllable = controllable;
        }

        /// <summary>
        /// Shots the variant's prefab to a texture using the <see cref="ObjectsShotCapture"/>
        /// </summary>
        private Texture2D ShotTexture()
        {
            var instance = ScenarioManager.Instance.GetExtension<PrefabsPools>().GetInstance(Prefab);
            var texture = ScenarioManager.Instance.objectsShotCapture.ShotObject(instance);
            ScenarioManager.Instance.GetExtension<PrefabsPools>().ReturnInstance(instance);
            return texture;
        }
    }
}
