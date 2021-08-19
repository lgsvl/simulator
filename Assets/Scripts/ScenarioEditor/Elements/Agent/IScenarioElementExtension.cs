/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using SimpleJSON;

    /// <summary>
    /// Base class for extending the scenario element parameters
    /// </summary>
    public interface IScenarioElementExtension
    {
        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="parentElement">Scenario element that this object extends</param>
        public void Initialize(ScenarioElement parentElement);

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize();

        /// <summary>
        /// Method that serializes current extension to the element node
        /// </summary>
        /// <param name="elementNode">Element node where object should be serialized</param>
        public void SerializeToJson(JSONNode elementNode);

        /// <summary>
        /// Method that deserializes extension from the element node
        /// </summary>
        /// <param name="elementNode">Element node where object is serialized</param>
        public void DeserializeFromJson(JSONNode elementNode);

        /// <summary>
        /// Method called after this element is instantiated using copied element
        /// </summary>
        /// <param name="originElement">Origin element from which copy was created</param>
        public void CopyProperties(ScenarioElement originElement);
    }
}
