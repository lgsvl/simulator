/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Utilities
{
    using System.Collections;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    public static class UIUtilities
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
            ScenarioManager.Instance.StartCoroutine(DelayedLayoutRebuild(transformToRebuild));
        }

        /// <summary>
        /// Method that rebuilds the UI layout after two frame update
        /// </summary>
        /// <param name="transformToRebuild">Transform that will be rebuilt</param>
        /// <returns>IEnumerator</returns>
        private static IEnumerator DelayedLayoutRebuild(RectTransform transformToRebuild)
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild (transformToRebuild);
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild (transformToRebuild);
        }
    }
}
