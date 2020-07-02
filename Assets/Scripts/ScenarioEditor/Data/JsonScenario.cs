/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data
{
    using SimpleJSON;

    /// <summary>
    /// Data set for the scenario serialized as json
    /// </summary>
    public class JsonScenario
    {
        /// <summary>
        /// Serialized scenario data as json
        /// </summary>
        private JSONNode scenarioData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioData">Serialized scenario data as json</param>
        public JsonScenario(JSONNode scenarioData)
        {
            this.scenarioData = scenarioData;
        }

        /// <summary>
        /// Serialized scenario data as json
        /// </summary>
        public JSONNode ScenarioData => scenarioData;
    }
}