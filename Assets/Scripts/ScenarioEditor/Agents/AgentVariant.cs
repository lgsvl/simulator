/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System.Threading.Tasks;
    using Managers;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Data describing a single agent variant of the scenario agent type
    /// </summary>
    public class AgentVariant
    {
        /// <summary>
        /// The source of the scenario agent type, this variant is a part of this source
        /// </summary>
        public ScenarioAgentSource source;

        /// <summary>
        /// Name of this agent variant
        /// </summary>
        public string name;

        /// <summary>
        /// Prefab used to visualize this agent variant
        /// </summary>
        public GameObject prefab;

        /// <summary>
        /// Texture used to visualize this agent variant in UI
        /// </summary>
        private Texture2D iconTexture;

        /// <summary>
        /// Sprite used to visualize this agent variant in UI
        /// </summary>
        private Sprite iconSprite;

        /// <summary>
        /// Texture used to visualize this agent variant in UI
        /// </summary>
        public Texture2D IconTexture
        {
            get
            {
                if (iconTexture == null)
                    iconTexture = ShotTexture();
                return iconTexture;
            }
        }

        /// <summary>
        /// Sprite used to visualize this agent variant in UI
        /// </summary>
        public Sprite IconSprite
        {
            get
            {
                if (iconSprite == null)
                    iconSprite = Sprite.Create(IconTexture, new Rect(0.0f, 0.0f, IconTexture.width, IconTexture.height),
                        new Vector2(0.5f, 0.5f), 100.0f);
                return iconSprite;
            }
        }

        /// <summary>
        /// Shots the variant's prefab to a texture using the <see cref="ObjectsShotCapture"/>
        /// </summary>
        /// <returns>Shot texture of this variant's prefab</returns>
        private Texture2D ShotTexture()
        {
            var instance = source.GetModelInstance(this);
            var texture = ScenarioManager.Instance.objectsShotCapture.ShotObject(instance);
            ScenarioManager.Instance.prefabsPools.ReturnInstance(instance);
            return texture;
        }

        /// <summary>
        /// Prepares the variant with all the assets
        /// </summary>
        /// <returns>Task</returns>
        #pragma warning disable 1998
        public virtual async Task Prepare()
        {
            ShotTexture();
        }
        #pragma warning restore 1998
    }
}