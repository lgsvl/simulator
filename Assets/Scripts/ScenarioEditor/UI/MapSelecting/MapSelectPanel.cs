/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapSelecting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Inspector;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Panel allows selecting different map for a scenario
    /// </summary>
    public class MapSelectPanel : MonoBehaviour, IInspectorContentPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Map select button sample
        /// </summary>
        [SerializeField]
        private MapSelectButton buttonSample;
#pragma warning restore 0649

        /// <summary>
        /// All the available map selection buttons
        /// </summary>
        private readonly List<MapSelectButton> buttons = new List<MapSelectButton>();

        /// <summary>
        /// Button corresponding currently loaded map
        /// </summary>
        private MapSelectButton currentMapButton;

        /// <inheritdoc/>
        public string MenuItemTitle => "Map";

        /// <summary>
        /// Unity Start method
        /// </summary>
        public void Start()
        {
            var mapManager = ScenarioManager.Instance.MapManager;
            mapManager.MapChanged += OnMapLoaded;
            var availableMaps = ScenarioManager.Instance.MapManager.AvailableMaps;
            var currentMapName = ScenarioManager.Instance.MapManager.CurrentMapName;
            for (var i = 0; i < availableMaps.Count; i++)
            {
                var availableMap = availableMaps[i];
                var mapSelectButton = Instantiate(buttonSample, buttonSample.transform.parent);
                mapSelectButton.Setup(availableMap.name);
                mapSelectButton.gameObject.SetActive(true);
                if (currentMapName == availableMap.name)
                {
                    mapSelectButton.MarkAsCurrent();
                    currentMapButton = mapSelectButton;
                }

                buttons.Add(mapSelectButton);
            }

            //Dynamic height
            var rectTransform = (RectTransform) transform;
            var buttonHeight = ((RectTransform) buttonSample.transform).sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, buttonHeight*availableMaps.Count);

            buttonSample.gameObject.SetActive(false);
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        public void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.MapManager.MapChanged -= OnMapLoaded;
        }

        /// <summary>
        /// Method called when new map is loaded
        /// </summary>
        /// <param name="mapName">The loaded map name</param>
        /// <exception cref="ArgumentException">There is no button corresponding to the loaded map</exception>
        private void OnMapLoaded(string mapName)
        {
            if (currentMapButton!=null)
                currentMapButton.UnmarkCurrent();
            var mapCorrespondingButton = buttons.Find((button) => button.MapName == mapName);
            if (mapCorrespondingButton == null)
                throw new ArgumentException("Could not find button corresponding to loaded map.");
            mapCorrespondingButton.MarkAsCurrent();
            currentMapButton = mapCorrespondingButton;
        }

        /// <inheritdoc/>
        void IInspectorContentPanel.Show()
        {
            gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        void IInspectorContentPanel.Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Selects map with the given name
        /// </summary>
        /// <param name="mapName">Name of map which should be loaded</param>
        public void SelectMap(string mapName)
        {
            ScenarioManager.Instance.RequestResetScenario(() =>
            {
                ScenarioManager.Instance.ShowLoadingPanel();
                //Delay selecting map so the loading panel can initialize
                var nonBlockingTask = DelayedSelectMap(mapName);
            }, null);
        }

        /// <summary>
        /// Coroutine invoking map load after a single frame update
        /// </summary>
        /// <param name="mapName">Name of map which should be loaded</param>
        /// <returns>IEnumerator</returns>
        private async Task DelayedSelectMap(string mapName)
        {
            await Task.Delay(20);
            await ScenarioManager.Instance.MapManager.LoadMapAsync(mapName);
            ScenarioManager.Instance.HideLoadingPanel();
        }
    }
}