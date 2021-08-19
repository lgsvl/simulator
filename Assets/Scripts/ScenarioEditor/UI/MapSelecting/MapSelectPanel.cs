/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapSelecting
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Inspector;
    using Managers;
    using ScenarioEditor.Utilities;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Panel allows selecting different map for a scenario
    /// </summary>
    public class MapSelectPanel : InspectorContentPanel
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
        public override void Initialize()
        {
            var nonBlockingTask = SetupButtons();
        }
        
        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (ScenarioManager.Instance == null)
                return;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            if (mapManager!=null)
                mapManager.MapChanged -= OnMapLoaded;
        }

        /// <summary>
        /// Setups maps buttons while waiting until map manager is initialized
        /// </summary>
        /// <returns>Task</returns>
        private async Task SetupButtons()
        {
            await ScenarioManager.Instance.WaitForExtension<ScenarioMapManager>();
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            mapManager.MapChanged += OnMapLoaded;
            var availableMaps = mapManager.AvailableMaps;
            var currentMapName = mapManager.CurrentMapName;
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
            var firstButtonY = ((RectTransform) buttons[0].transform).anchoredPosition.y;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, (buttonHeight+4)*availableMaps.Count-firstButtonY);

            buttonSample.gameObject.SetActive(false);
            UnityUtilities.LayoutRebuild(rectTransform);
            rectTransform.offsetMin = Vector2.zero;
        }

        /// <summary>
        /// Method called when new map is loaded
        /// </summary>
        /// <param name="mapMetaData">The loaded map meta data</param>
        /// <exception cref="ArgumentException">There is no button corresponding to the loaded map</exception>
        private void OnMapLoaded(ScenarioMapManager.MapMetaData mapMetaData)
        {
            if (currentMapButton!=null)
                currentMapButton.UnmarkCurrent();
            var mapCorrespondingButton = buttons.Find((button) => button.MapName == mapMetaData.name);
            if (mapCorrespondingButton == null)
                throw new ArgumentException("Could not find button corresponding to loaded map.");
            mapCorrespondingButton.MarkAsCurrent();
            currentMapButton = mapCorrespondingButton;
        }

        /// <inheritdoc/>
        public override void Show()
        {
            gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        public override void Hide()
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
                var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
                loadingProcess.Update($"Switching to the {mapName} map.");
                //Delay selecting map so the loading panel can initialize
                var nonBlockingTask = DelayedSelectMap(mapName, loadingProcess);
            }, null);
        }

        /// <summary>
        /// Coroutine invoking map load after a single frame update
        /// </summary>
        /// <param name="mapName">Name of map which should be loaded</param>
        /// <returns>Coroutine IEnumerator</returns>
        private async Task DelayedSelectMap(string mapName, LoadingPanel.LoadingProcess loadingProcess)
        {
            await Task.Delay(20);
            await ScenarioManager.Instance.GetExtension<ScenarioMapManager>().LoadMapAsync(mapName);
            loadingProcess.Update($"Switched to the {mapName} map.");
            loadingProcess.NotifyCompletion();
        }
    }
}