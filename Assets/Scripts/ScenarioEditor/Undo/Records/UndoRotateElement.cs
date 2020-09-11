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
    /// Record that undoes rotating scenario element on the map
    /// </summary>
    public class UndoRotateElement : UndoRecord
    {
        /// <summary>
        /// Scenario element that was rotated on the map
        /// </summary>
        private readonly ScenarioElement scenarioElement;

        /// <summary>
        /// Rotation of the scenario element before the rotating
        /// </summary>
        private readonly Quaternion previousRotation;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was rotated on the map</param>
        /// <param name="previousRotation">Rotation of the scenario element before the rotating</param>
        public UndoRotateElement(ScenarioElement scenarioElement, Quaternion previousRotation)
        {
            this.scenarioElement = scenarioElement;
            this.previousRotation = previousRotation;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioElement.ForceRotate(previousRotation);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback element rotation.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
