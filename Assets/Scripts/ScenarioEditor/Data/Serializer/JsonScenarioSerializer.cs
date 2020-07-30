/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data.Serializer
{
    using Agents;
    using Elements;
    using Managers;
    using SimpleJSON;
    using UI.MapSelecting;
    using UnityEngine;

    /// <summary>
    /// Class serializing a scenario into a json data
    /// </summary>
    public static class JsonScenarioSerializer
    {
        /// <summary>
        /// Serializes current scenario state into a json scenario
        /// </summary>
        /// <returns>Json scenario with serialized scenario</returns>
        public static JsonScenario SerializeScenario()
        {
            var scenarioData = new JSONObject();
            var scenarioManager = ScenarioManager.Instance;
            scenarioData.Add("version", new JSONString("0.01"));
            var vseMetadata = new JSONObject();
            scenarioData.Add("vse_metadata", vseMetadata);
            SerializeMetadata(vseMetadata);
            AddMapNode(scenarioData, scenarioManager.MapManager.CurrentMapName);
            var agents = scenarioManager.GetComponentsInChildren<ScenarioAgent>();
            foreach (var agent in agents)
            {
                AddAgentNode(scenarioData, agent);
            }

            return new JsonScenario(scenarioData);
        }

        /// <summary>
        /// Adds visual scenario editor metadata to the json scenario
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        private static void SerializeMetadata(JSONObject data)
        {
            var cameraSettings = new JSONObject();
            data.Add("camera_settings", cameraSettings);
            var camera = ScenarioManager.Instance.ScenarioCamera;
            var position = new JSONObject().WriteVector3(camera.transform.position);
            cameraSettings.Add("position", position);
        }

        /// <summary>
        /// Adds map node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="mapName">Scenario map to serialize</param>
        private static void AddMapNode(JSONObject data, string mapName)
        {
            var map = new JSONObject();
            data.Add("map", map);
            map.Add("name", new JSONString(mapName));
        }

        /// <summary>
        /// Adds an agent node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioAgent">Scenario agent to serialize</param>
        private static void AddAgentNode(JSONObject data, ScenarioAgent scenarioAgent)
        {
            var agents = data.GetValueOrDefault("agents", new JSONArray());
            if (!data.HasKey("agents"))
                data.Add("agents", agents);
            var agent = new JSONObject();
            agents.Add(agent);
            agent.Add("uid", new JSONString(scenarioAgent.Uid));
            agent.Add("variant", new JSONString(scenarioAgent.Variant.name));
            agent.Add("type", new JSONNumber(scenarioAgent.Source.AgentTypeId));
            var transform = new JSONObject();
            agent.Add("transform", transform);
            var position = new JSONObject().WriteVector3(scenarioAgent.TransformToMove.position);
            transform.Add("position", position);
            var rotation = new JSONObject().WriteVector3(scenarioAgent.TransformToRotate.rotation.eulerAngles);
            transform.Add("rotation", rotation);
            AddWaypointsNodes(agent, scenarioAgent);
        }

        /// <summary>
        /// Adds waypoints nodes to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioAgent">Scenario agent which includes those waypoints</param>
        private static void AddWaypointsNodes(JSONObject data, ScenarioAgent scenarioAgent)
        {
            var waypoints = data.GetValueOrDefault("waypoints", new JSONArray());
            if (!data.HasKey("waypoints"))
                data.Add("waypoints", waypoints);

            var angle = Vector3.zero;
            for (var i = 0; i < scenarioAgent.Waypoints.Count; i++)
            {
                var scenarioWaypoint = scenarioAgent.Waypoints[i];
                var waypointNode = new JSONObject();
                var position = new JSONObject().WriteVector3(scenarioWaypoint.transform.position);
                var hasNextWaypoint = i + 1 < scenarioAgent.Waypoints.Count;
                angle = hasNextWaypoint
                    ? Quaternion.LookRotation(scenarioAgent.Waypoints[i + 1].transform.position - position).eulerAngles
                    : angle;
                waypointNode.Add("ordinal_number", new JSONNumber(i));
                waypointNode.Add("position", position);
                waypointNode.Add("angle", angle);
                waypointNode.Add("wait_time", new JSONNumber(scenarioWaypoint.WaitTime));
                waypointNode.Add("speed", new JSONNumber(scenarioWaypoint.Speed));
                AddTriggerNode(waypointNode, scenarioWaypoint.LinkedTrigger);
                waypoints.Add(waypointNode);
            }
        }

        /// <summary>
        /// Adds triggers nodes to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioTrigger">Scenario trigger to serialize</param>
        private static void AddTriggerNode(JSONObject data, ScenarioTrigger scenarioTrigger)
        {
            var triggerNode = new JSONObject();
            var effectorsArray = new JSONArray();;
            triggerNode.Add("effectors", effectorsArray);
            foreach (var effector in scenarioTrigger.Trigger.Effectors)
            {
                var effectorNode = new JSONObject();
                effectorNode.Add("typeName", new JSONString(effector.TypeName));
                var parameters = new JSONObject();
                effectorNode.Add("parameters", parameters);
                effector.SerializeProperties(parameters);
                effectorsArray.Add(effectorNode);
            }
            data.Add("trigger", triggerNode);
        }
    }
}