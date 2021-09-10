/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data
{
    using System.Threading.Tasks;
    using SimpleJSON;

    /// <summary>
    /// Interface for all the scenario managers that extends the visual scenario editor
    /// </summary>
    public interface ISerializedExtension
    {
        /// <summary>
        /// Serializes the extension's data into given <see cref="JSONNode"/>
        /// </summary>
        /// <param name="data"><see cref="JSONNode"/> where extension data will be serialized</param>
        bool Serialize(JSONNode data);

        /// <summary>
        /// Deserializes the extension's data from given <see cref="JSONNode"/>
        /// </summary>
        /// <param name="data"><see cref="JSONNode"/> where extension's data is serialized</param>
        Task<bool> Deserialize(JSONNode data);
    }
}