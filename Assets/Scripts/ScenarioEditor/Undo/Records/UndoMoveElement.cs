/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Elements;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Record that undoes moving scenario element on the map
    /// </summary>
    public class UndoMoveElement : UndoRecord
    {
        /// <summary>
        /// Scenario element that was moved on the map
        /// </summary>
        private readonly ScenarioElement scenarioElement;

        /// <summary>
        /// Position of the scenario element before the movement
        /// </summary>
        private readonly Vector3 previousPosition;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was moved on the map</param>
        /// <param name="previousPosition">Position of the scenario element before the movement</param>
        public UndoMoveElement(ScenarioElement scenarioElement, Vector3 previousPosition)
        {
            this.scenarioElement = scenarioElement;
            this.previousPosition = previousPosition;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioElement.ForceMove(previousPosition);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback element movement.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
