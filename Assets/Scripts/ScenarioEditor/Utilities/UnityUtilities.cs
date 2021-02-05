/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Utilities
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Class that has some common methods used to handle Unity engine
    /// </summary>
    public static class UnityUtilities
    {
        /// <summary>
        /// Method that rebuilds the UI layout after two frame updates
        /// </summary>
        /// <param name="transformToRebuild">Transform that will be rebuilt</param>
        public static void LayoutRebuild(RectTransform transformToRebuild)
        {
            if (transformToRebuild == null)
                return;
            //Layout rebuild is required after one frame when content changes size
            LayoutRebuilder.ForceRebuildLayoutImmediate(transformToRebuild);
            do
            {
                transformToRebuild = transformToRebuild.parent as RectTransform;
                if (transformToRebuild != null && (transformToRebuild.GetComponent<ContentSizeFitter>() != null ||
                                                   transformToRebuild.GetComponent<LayoutGroup>() != null))
                    LayoutRebuilder.ForceRebuildLayoutImmediate(transformToRebuild);
            } while (transformToRebuild != null);
        }
    }
}
