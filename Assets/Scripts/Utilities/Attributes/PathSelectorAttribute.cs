/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities.Attributes
{
    using UnityEngine;

    /// <summary>
    /// Attribute that will draw button for opening file selection window next to string field. 
    /// </summary>
    public class PathSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// If true, button will open directory selection window instead of file selection window.
        /// </summary>
        public bool SelectDirectory;

        /// <summary>
        /// Comma-separated extensions that should be allowed for selection. 
        /// </summary>
        public string AllowedExtensions;

        /// <summary>
        /// If true, only path relative to Assets folder will be stored if file or directory is in there.
        /// </summary>
        public bool TruncateToRelative;
    }
}