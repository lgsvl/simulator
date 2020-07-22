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
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Agent source panel visualize a single agent type for adding
    /// </summary>
    public class AgentSourcePanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Title text of this panel
        /// </summary>
        [SerializeField]
        private Text title;

        /// <summary>
        /// Main image of this panel
        /// </summary>
        [SerializeField]
        private RawImage image;
#pragma warning restore 0649

        /// <summary>
        /// Cached agent source class which is used for adding agent from this panel
        /// </summary>
        private ScenarioAgentSource agentSource;

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="source">Agent source class which will be used for adding agent from this panel</param>
        public void Initialize(ScenarioAgentSource source)
        {
            agentSource = source;
            title.text = source?.AgentTypeName;

            if (agentSource != null)
            {
                var nonBlockingTask = SetupTexture();
            }
        }

        /// <summary>
        /// Setups the texture of this panel asynchronously waiting until the texture is ready
        /// </summary>
        /// <returns>Task</returns>
        private async Task SetupTexture()
        {
            while (agentSource.DefaultVariant == null)
                await Task.Delay(25);
            image.texture = agentSource.DefaultVariant.IconTexture;
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            agentSource?.Deinitialize();
        }

        /// <summary>
        /// Request dragging a new agent from the linked source to the scenario
        /// </summary>
        public void DragNewAgent()
        {
            agentSource.DragNewAgent();
        }
    }
}