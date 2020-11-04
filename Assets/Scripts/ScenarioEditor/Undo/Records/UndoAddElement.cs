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

    /// <summary>
    /// Record that undoes adding an scenario element to the map
    /// </summary>
    public class UndoAddElement : UndoRecord
    {
        /// <summary>
        /// Scenario element that was added to the map
        /// </summary>
        private readonly ScenarioElement scenarioElement;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was added to the map</param>
        public UndoAddElement(ScenarioElement scenarioElement)
        {
            this.scenarioElement = scenarioElement;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            var elementType = scenarioElement.ElementType;
            scenarioElement.RemoveFromMap();
            scenarioElement.Dispose();
            ScenarioManager.Instance.logPanel.EnqueueInfo($"Undo applied to rollback adding a scenario element of type: {elementType}.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}