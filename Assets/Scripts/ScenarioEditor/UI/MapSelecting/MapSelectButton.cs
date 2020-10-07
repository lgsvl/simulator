/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.MapSelecting
{
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Button for selecting a different map in the scenario editor
    /// </summary>
    public class MapSelectButton : MonoBehaviour
    {
        /// <summary>
        /// Sign that is added to the name text when bound map is unprepared
        /// </summary>
        private static string UnpreparedSign = "";
        
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Parent <see cref="MapSelectPanel"/> that handles the map selection
        /// </summary>
        [SerializeField]
        private MapSelectPanel mapSelectPanel;

        /// <summary>
        /// UI button of this map selection button
        /// </summary>
        [SerializeField]
        private Button uiButton;

        /// <summary>
        /// Text object displaying the map name
        /// </summary>
        [SerializeField]
        private Text nameText;
#pragma warning restore 0649
        
        /// <summary>
        /// Is the corresponding map currently loaded
        /// </summary>
        private bool isCurrentMap;

        /// <summary>
        /// Corresponding map name
        /// </summary>
        private string mapName;

        /// <summary>
        /// Corresponding map name
        /// </summary>
        public string MapName
        {
            get => mapName;
            private set => mapName = value;
        }

        /// <summary>
        /// Setups the button with selected map name
        /// </summary>
        /// <param name="map">Map name that will be bound to this button</param>
        public void Setup(string map)
        {
            MapName = map;
            var isMapDownloaded = ScenarioManager.Instance.GetExtension<ScenarioMapManager>().IsMapDownloaded(map);
            nameText.text = !isMapDownloaded ? $"{UnpreparedSign} {map} {UnpreparedSign}" : map;
        }

        /// <summary>
        /// Selects the corresponding map
        /// </summary>
        public void SelectMap()
        {
            if (!isCurrentMap)
                mapSelectPanel.SelectMap(MapName);
        }

        /// <summary>
        /// Marks this button - corresponding map is currently loaded
        /// </summary>
        public void MarkAsCurrent()
        {
            uiButton.interactable = false;
            isCurrentMap = true;
            nameText.text = MapName;
        }

        /// <summary>
        /// Unmarks this button - corresponding map is no longer loaded
        /// </summary>
        public void UnmarkCurrent()
        {
            uiButton.interactable = true;
            isCurrentMap = false;
        }
    }
}