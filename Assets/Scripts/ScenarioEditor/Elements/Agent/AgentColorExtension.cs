/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using System;
    using ScenarioEditor.Agents;
    using SimpleJSON;
    using UnityEngine;

    /// <summary>
    /// Scenario agent extension that handles the color
    /// </summary>
    public class AgentColorExtension : ScenarioAgentExtension
    {
        /// <summary>
        /// Id of the shader property named _BaseColor
        /// </summary>
        public static readonly int BaseColorShaderId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Color of this agent
        /// </summary>
        private Color agentColor;

        /// <summary>
        /// Color of this agent if it supports changing the color
        /// </summary>
        public Color AgentColor
        {
            get => agentColor;
            set
            {
                agentColor = value;
                foreach (var modelRenderer in ParentAgent.ModelRenderers)
                {
                    foreach (var material in modelRenderer.materials)
                        if (material.name.Contains("Body"))
                            material.SetColor(BaseColorShaderId, agentColor);
                }

                ColorChanged?.Invoke(agentColor);
            }
        }

        /// <summary>
        /// Initial color of this agent
        /// </summary>
        public Color InitialColor { get; set; }

        /// <summary>
        /// Event invoked when this agent changes the color
        /// </summary>
        public event Action<Color> ColorChanged;

        /// <inheritdoc/>
        public override void Initialize(ScenarioAgent parentAgent)
        {
            base.Initialize(parentAgent);
            ParentAgent.VariantChanged += ParentAgentOnVariantChanged;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            AgentColor = InitialColor;
            ParentAgent.VariantChanged -= ParentAgentOnVariantChanged;
            base.Deinitialize();
        }


        /// <inheritdoc/>
        public override void SerializeToJson(JSONNode agentNode)
        {
            var colorNode = new JSONObject();
            agentNode.Add("color", colorNode);
            colorNode.Add("r", new JSONNumber(AgentColor.r));
            colorNode.Add("g", new JSONNumber(AgentColor.g));
            colorNode.Add("b", new JSONNumber(AgentColor.b));
        }

        /// <inheritdoc/>
        public override void DeserializeFromJson(JSONNode agentNode)
        {
            var colorNode = agentNode["color"];
            AgentColor = new Color(colorNode["r"].AsFloat, colorNode["g"].AsFloat, colorNode["b"].AsFloat);
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioAgent agent)
        {
            var origin = agent.GetExtension<AgentColorExtension>();
            if (origin == null) return;
            InitialColor = origin.InitialColor;
            AgentColor = origin.AgentColor;
        }

        /// <summary>
        /// Method called when variant of the parent agent changes
        /// </summary>
        /// <param name="newVariant">New variant of the parent agent</param>
        private void ParentAgentOnVariantChanged(SourceVariant newVariant)
        {
            foreach (var modelRenderer in ParentAgent.ModelRenderers)
            {
                //Search for the initial color
                var colorSet = false;
                foreach (var material in modelRenderer.materials)
                    if (material.name.Contains("Body"))
                    {
                        InitialColor = material.GetColor(BaseColorShaderId);
                        if (Mathf.Approximately(InitialColor.a, 0.0f)) continue;
                        colorSet = true;
                        break;
                    }

                if (colorSet)
                    break;
            }

            AgentColor = Mathf.Approximately(InitialColor.a, 0.0f) ? Color.white : InitialColor;
        }
    }
}