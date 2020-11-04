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
    /// Record that undoes removing scenario element from the map
    /// </summary>
    public class UndoRemoveElement : UndoRecord
    {
        /// <summary>
        /// Scenario element that was removed from the map
        /// </summary>
        private readonly ScenarioElement scenarioElement;

        /// <summary>
        /// Cached scenario element parent, required to reparent on undo
        /// </summary>
        private readonly Transform previousParent;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioElement">Scenario element that was removed from the map</param>
        public UndoRemoveElement(ScenarioElement scenarioElement)
        {
            this.scenarioElement = scenarioElement;
            GameObject gameObject;
            (gameObject = scenarioElement.gameObject).SetActive(false);
            previousParent = gameObject.transform.parent;
            scenarioElement.transform.SetParent(ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().transform);
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            var elementType = scenarioElement.ElementType;
            scenarioElement.transform.SetParent(previousParent);
            scenarioElement.gameObject.SetActive(true);
            scenarioElement.UndoRemove();
            ScenarioManager.Instance.logPanel.EnqueueInfo($"Undo applied to rollback removing a scenario element of type: {elementType}.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            scenarioElement.Dispose();
        }
    }
}