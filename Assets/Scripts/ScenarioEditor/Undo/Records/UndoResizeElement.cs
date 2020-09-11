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
    /// Record that undoes resizing scenario element on the map
    /// </summary>
    public class UndoResizeElement : UndoRecord
    {
        /// <summary>
        /// Scenario element that was resized on the map
        /// </summary>
        private readonly ScenarioElement scenarioElement;

        /// <summary>
        /// Scale of the scenario element before the resizing
        /// </summary>
        private readonly Vector3 previousScale;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was resized on the map</param>
        /// <param name="previousScale">Scale of the scenario element before the resizing</param>
        public UndoResizeElement(ScenarioElement scenarioElement, Vector3 previousScale)
        {
            this.scenarioElement = scenarioElement;
            this.previousScale = previousScale;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioElement.ForceResize(previousScale);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback element resizing.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
