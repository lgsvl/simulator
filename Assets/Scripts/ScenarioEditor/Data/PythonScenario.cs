/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data
{
    /// <summary>
    /// Data set for the scenario serialized as Python API script
    /// </summary>
    public class PythonScenario
    {
        /// <summary>
        /// Serialized scenario data as Python API script
        /// </summary>
        private string scenarioData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioData">Serialized scenario data as Python API script</param>
        public PythonScenario(string scenarioData)
        {
            this.scenarioData = scenarioData;
        }

        /// <summary>
        /// Serialized scenario data as Python API script
        /// </summary>
        public string ScenarioData => scenarioData;
    }
}