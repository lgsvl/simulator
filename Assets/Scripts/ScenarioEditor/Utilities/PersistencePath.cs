/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Utilities
{
    using UnityEngine;

    /// <summary>
    /// File path that is saved in the persistence data
    /// </summary>
    public class PersistencePath
    {
        /// <summary>
        /// Key used to save value in the persistence data
        /// </summary>
        private readonly string key;
        
        /// <summary>
        /// File path
        /// </summary>
        private string path;

        /// <summary>
        /// File path
        /// </summary>
        public string Value
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                    path = PlayerPrefs.GetString(key, Application.persistentDataPath);
                return path;
            }
            set
            {
                path = value;
                PlayerPrefs.SetString(key, value);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">Key used to save value in the persistence data</param>
        public PersistencePath(string key)
        {
            this.key = key;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value;
        }
    }
}