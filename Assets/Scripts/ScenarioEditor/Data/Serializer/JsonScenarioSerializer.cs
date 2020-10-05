/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data.Serializer
{
    using System.Collections;
    using Agents;
    using Controllables;
    using Elements;
    using Elements.Agent;
    using Managers;
    using SimpleJSON;
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
            AddMapNode(scenarioData, scenarioManager.GetExtension<ScenarioMapManager>().CurrentMapMetaData);
            //Add agents
            var agents = scenarioManager.GetExtension<ScenarioAgentsManager>().Agents;
            var agentsNode = scenarioData.GetValueOrDefault("agents", new JSONArray());
            if (!scenarioData.HasKey("agents"))
                scenarioData.Add("agents", agentsNode);
            foreach (var agent in agents)
                AddAgentNode(agentsNode, agent);
            //Add controllables
            var controllables = scenarioManager.GetExtension<ScenarioControllablesManager>().Controllables;
            var controllablesNode = scenarioData.GetValueOrDefault("controllables", new JSONArray());
            if (!scenarioData.HasKey("controllables"))
                scenarioData.Add("controllables", controllablesNode);
            foreach (var controllable in controllables)
                AddControllableNode(controllablesNode, controllable);

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
            var rotation = new JSONObject().WriteVector3(camera.transform.rotation.eulerAngles);
            cameraSettings.Add("rotation", rotation);
        }

        /// <summary>
        /// Adds map node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioMap">Scenario map to serialize</param>
        private static void AddMapNode(JSONObject data, ScenarioMapManager.MapMetaData scenarioMap)
        {
            var mapNode = new JSONObject();
            data.Add("map", mapNode);
            mapNode.Add("id", new JSONString(scenarioMap.guid));
            mapNode.Add("name", new JSONString(scenarioMap.name));
            mapNode.Add("parameterType", new JSONString("map"));
        }

        /// <summary>
        /// Adds an agent node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioAgent">Scenario agent to serialize</param>
        private static void AddAgentNode(JSONNode data, ScenarioAgent scenarioAgent)
        {
            var agentNode = new JSONObject();
            data.Add(agentNode);
            if (scenarioAgent.Variant is CloudAgentVariant cloudVariant)
                agentNode.Add("id", new JSONString(cloudVariant.guid));
            agentNode.Add("uid", new JSONString(scenarioAgent.Uid));
            agentNode.Add("variant", new JSONString(scenarioAgent.Variant.name));
            agentNode.Add("type", new JSONNumber(scenarioAgent.Source.AgentTypeId));
            agentNode.Add("parameterType", new JSONString(""));
            var transform = new JSONObject();
            agentNode.Add("transform", transform);
            var position = new JSONObject().WriteVector3(scenarioAgent.TransformToMove.position);
            transform.Add("position", position);
            var rotation = new JSONObject().WriteVector3(scenarioAgent.TransformToRotate.rotation.eulerAngles);
            transform.Add("rotation", rotation);
            if (!string.IsNullOrEmpty(scenarioAgent.Behaviour))
            {
                var behaviour = new JSONObject();
                behaviour.Add("name", new JSONString(scenarioAgent.Behaviour));
                agentNode.Add("behaviour", behaviour);
                if (scenarioAgent.BehaviourParameters.Count > 0)
                    behaviour.Add("parameters", scenarioAgent.BehaviourParameters);
            }

            if (scenarioAgent.DestinationPoint != null)
            {
                var destinationPoint = new JSONObject().WriteVector3(scenarioAgent.DestinationPoint.transform.position);
                agentNode.Add("destinationPoint", destinationPoint);
            }
            if (scenarioAgent.Source.AgentSupportWaypoints(scenarioAgent))
                AddWaypointsNodes(agentNode, scenarioAgent);
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
        
        /// <summary>
        /// Adds an controllable node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="controllable">Scenario controllable to serialize</param>
        private static void AddControllableNode(JSONNode data, ScenarioControllable controllable)
        {
            var controllableNode = new JSONObject();
            data.Add(controllableNode);
            controllableNode.Add("uid", new JSONString(controllable.Uid));
            controllableNode.Add("name", new JSONString(controllable.Variant.name));
            var transform = new JSONObject();
            controllableNode.Add("transform", transform);
            var position = new JSONObject().WriteVector3(controllable.TransformToMove.position);
            transform.Add("position", position);
            var rotation = new JSONObject().WriteVector3(controllable.TransformToRotate.rotation.eulerAngles);
            transform.Add("rotation", rotation);
        }
    }
}
