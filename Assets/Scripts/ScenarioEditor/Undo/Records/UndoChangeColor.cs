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
        /// Scenario agent which color was changed
        /// </summary>
        private ScenarioAgent scenarioAgent;

        /// <summary>
        /// Previous agent color
        /// </summary>
        private Color previousColor;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioAgent">Scenario agent which variant was changed</param>
        /// <param name="previousColor">Previous color of the given agent</param>
        public UndoChangeColor(ScenarioAgent scenarioAgent, Color previousColor)
        {
            this.scenarioAgent = scenarioAgent;
            this.previousColor = previousColor;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioAgent.AgentColor = previousColor;
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent color.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
