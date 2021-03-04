/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Elements.Agents;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Record that undoes changing the agent color
    /// </summary>
    public class UndoChangeColor : UndoRecord
    {
        /// <summary>
        /// Scenario agent color extension which color was changed
        /// </summary>
        private AgentColorExtension agentColor;

        /// <summary>
        /// Previous agent color
        /// </summary>
        private Color previousColor;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="agentColor">Scenario agent color extension which color was changed</param>
        /// <param name="previousColor">Previous color of the given agent</param>
        public UndoChangeColor(AgentColorExtension agentColor, Color previousColor)
        {
            this.agentColor = agentColor;
            this.previousColor = previousColor;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            agentColor.AgentColor = previousColor;
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent color.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
