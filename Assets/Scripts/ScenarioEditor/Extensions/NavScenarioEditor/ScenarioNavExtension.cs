/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Nav
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Agents;
    using Data;
    using Elements;
    using Managers;
    using Map;
    using SimpleJSON;
    using UnityEngine;

    /// <summary>
    /// Extension that handles and serializes Nav elements like <see cref="ScenarioNavOrigin"/>
    /// </summary>
    public class ScenarioNavExtension : MonoBehaviour, IScenarioEditorExtension, ISerializedExtension
    {
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Prefab for instantiating the nav origin models
        /// </summary>
        [SerializeField]
        private NavOrigin NavOriginPrefab; 

        /// <summary>
        /// Created nav source for this extension
        /// </summary>
        private ScenarioNavSource source;

        /// <summary>
        /// Source variant of the <see cref="ScenarioNavOrigin"/> element
        /// </summary>
        private SourceVariant navOriginVariant;

        /// <summary>
        /// <see cref="ScenarioNavOrigin"/> registered in this extension
        /// </summary>
        private readonly List<ScenarioNavOrigin> navOrigins = new List<ScenarioNavOrigin>();

        /// <summary>
        /// Default positions of the nav origins bound to map
        /// </summary>
        private readonly List<Vector3> mapNavOriginsPositions = new List<Vector3>();

        /// <inheritdoc/>
        public Task Initialize()
        {
            var sourceGO = new GameObject("NavOriginSource");
            sourceGO.transform.SetParent(transform);
            source = sourceGO.AddComponent<ScenarioNavSource>();
            navOriginVariant = new SourceVariant("ScenarioNavOrigin", "This is a scenario NavOrigin implementation.", NavOriginPrefab.gameObject);
            source.Variants.Add(navOriginVariant);
            ScenarioManager.Instance.ScenarioReset += OnScenarioReset;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            OnMapChanged(mapManager.CurrentMapMetaData);
            mapManager.MapChanged += OnMapChanged;
            IsInitialized = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            for (var i = navOrigins.Count - 1; i >= 0; i--)
            {
                var navOrigin = navOrigins[i];
                navOrigin.RemoveFromMap();
                navOrigin.Dispose();
            }
            navOrigins.Clear();
            mapNavOriginsPositions.Clear();
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            mapManager.MapChanged -= OnMapChanged;
            ScenarioManager.Instance.ScenarioReset -= OnScenarioReset;
        }

        /// <summary>
        /// Method invoked when current scenario is being reset
        /// </summary>
        private void OnScenarioReset()
        {
            for (var i = navOrigins.Count - 1; i >= 0; i--)
            {
                var navOrigin = navOrigins[i];
                if (navOrigin.BoundToMap)
                {
                    navOrigin.TransformToMove.position = mapNavOriginsPositions[i];
                }
                else
                {
                    navOrigin.RemoveFromMap();
                    navOrigin.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Method called when new map is loaded
        /// </summary>
        /// <param name="mapData">Loaded map data</param>
        private void OnMapChanged(ScenarioMapManager.MapMetaData mapData)
        {
            for (var i = navOrigins.Count - 1; i >= 0; i--)
            {
                var navOrigin = navOrigins[i];
                navOrigin.RemoveFromMap();
                navOrigin.Dispose();
            }
            navOrigins.Clear();
            mapNavOriginsPositions.Clear();

            LoadMapNavOrigins();
        }
        
        /// <summary>
        /// Loads nav origins objects from the map
        /// </summary>
        private void LoadMapNavOrigins()
        {
            var mapNavOrigins = FindObjectsOfType<NavOrigin>();
            if (mapNavOrigins.Length <= 0) return;
            foreach (var navOrigin in mapNavOrigins)
            {
                if (source.GetElementInstance(navOriginVariant) is ScenarioNavOrigin scenarioNavOrigin)
                {
                    scenarioNavOrigin.Setup(navOrigin, true);
                    var position = navOrigin.transform.position;
                    mapNavOriginsPositions.Add(position);
                    scenarioNavOrigin.ForceMove(position);
                }
            }
        }

        /// <summary>
        /// Registers the <see cref="ScenarioNavOrigin"/> in the extension, for example for the serialization
        /// </summary>
        /// <param name="navOrigin"><see cref="ScenarioNavOrigin"/> that will be registered</param>
        public void RegisterNavOrigin(ScenarioNavOrigin navOrigin)
        {
            if (!navOrigins.Contains(navOrigin))
            {
                if (navOrigins.Count > 0)
                {
                    ScenarioManager.Instance.logPanel.EnqueueWarning($"Scenario editor supports only one {nameof(NavOrigin)}.");
                    navOrigin.RemoveFromMap();
                    navOrigin.Dispose();
                }
                else
                    navOrigins.Add(navOrigin);
            }
        }
        
        /// <summary>
        /// Unregisters the <see cref="ScenarioNavOrigin"/> from the extension
        /// </summary>
        /// <param name="navOrigin"><see cref="ScenarioNavOrigin"/> that will be unregistered</param>
        public void UnregisterNavOrigin(ScenarioNavOrigin navOrigin)
        {
            navOrigins.Remove(navOrigin);
        }

        /// <summary>
        /// Gets an instantiated variant if it is available in the scenario
        /// </summary>
        /// <param name="variant">Requested source variant</param>
        /// <returns>Instantiated variant if available, null otherwise</returns>
        public ScenarioElement GetVariantInstance(SourceVariant variant)
        {
            if (variant == navOriginVariant)
            {
                return navOrigins.Count > 0 ? navOrigins[0] : null;
            }

            return null;
        }

        /// <inheritdoc/>
        bool ISerializedExtension.Serialize(JSONNode data)
        {
            var navDataObject = data.GetValueOrDefault("navData", new JSONObject());
            if (!data.HasKey("navData"))
                data.Add("navData", navDataObject);
            var navOriginsArray = navDataObject.GetValueOrDefault("navOrigins", new JSONArray());
            if (!navDataObject.HasKey("navOrigins"))
                navDataObject.Add("navOrigins", navOriginsArray);
            //Serialize nav origin points
            foreach (var point in navOrigins)
            {
                SerializeNavOrigin(navOriginsArray, point);
            }

            return true;
        }

        /// <summary>
        /// Serializes the <see cref="ScenarioNavOrigin"/> to the data node
        /// </summary>
        /// <param name="data"><see cref="JSONNode"/> where the data will be serialized</param>
        /// <param name="scenarioNavOrigin"><see cref="ScenarioNavOrigin"/> that will be serialized</param>
        private void SerializeNavOrigin(JSONNode data, ScenarioNavOrigin scenarioNavOrigin)
        {
            var navOriginNode = new JSONObject();
            data.Add(navOriginNode);
            var transformNode = new JSONObject();
            navOriginNode.Add("transform", transformNode);
            var position = new JSONObject().WriteVector3(scenarioNavOrigin.TransformToMove.position);
            transformNode.Add("position", position);
            var rotation = new JSONObject().WriteVector3(scenarioNavOrigin.TransformToRotate.rotation.eulerAngles);
            transformNode.Add("rotation", rotation);

            var parametersNode = new JSONObject();
            navOriginNode.Add("parameters", parametersNode);
            var originX = new JSONNumber(scenarioNavOrigin.NavOrigin.OriginX);
            parametersNode.Add("originX", originX);
            var originY = new JSONNumber(scenarioNavOrigin.NavOrigin.OriginY);
            parametersNode.Add("originY", originY);
            var rotationParameter = new JSONNumber(scenarioNavOrigin.NavOrigin.Rotation);
            parametersNode.Add("rotation", rotationParameter);
        }

        /// <inheritdoc/>
        Task<bool> ISerializedExtension.Deserialize(JSONNode data)
        {
            var navDataObject = data["navData"] as JSONObject;
            if (navDataObject == null)
                return Task.FromResult(true);
            var navOriginsArray = navDataObject["navOrigins"] as JSONArray;
            if (navOriginsArray == null)
                return Task.FromResult(true);
            var index = 0;
            foreach (var pointNode in navOriginsArray.Children)
            {
                ScenarioNavOrigin newPoint;
                
                // First fill map nav origins with data, then instantiate new ones
                if (index < navOrigins.Count)
                    newPoint = navOrigins[index];
                else
                {
                    newPoint = source.GetElementInstance(navOriginVariant) as ScenarioNavOrigin;
                    if (newPoint == null)
                        continue;
                    newPoint.Setup(newPoint.GetComponent<NavOrigin>(), false);
                }

                var transformNode = pointNode["transform"];
                newPoint.TransformToMove.position = transformNode["position"].ReadVector3();
                newPoint.TransformToRotate.rotation = Quaternion.Euler(transformNode["rotation"].ReadVector3());
                var parametersNode = pointNode["parameters"];
                newPoint.NavOrigin.OriginX = parametersNode["originX"];
                newPoint.NavOrigin.OriginY = parametersNode["originY"];
                newPoint.NavOrigin.Rotation = parametersNode["rotation"];
                index++;
            }

            return Task.FromResult(true);
        }
    }
}