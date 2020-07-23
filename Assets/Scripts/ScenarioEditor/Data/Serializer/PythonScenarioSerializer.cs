/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data.Serializer
{
    using System.Text;
    using Agents;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Class serializing a scenario into a Python API scenario
    /// </summary>
    public static class PythonScenarioSerializer
    {
        /// <summary>
        /// Current indent level in the python script
        /// </summary>
        private static int indentLevel = 0;

        /// <summary>
        /// Serializes current scenario state into a Python API script
        /// </summary>
        /// <returns>Python API scenario with serialized scenario</returns>
        public static PythonScenario SerializeScenario()
        {
            var stringBuilder = new StringBuilder();
            var scenarioManager = ScenarioManager.Instance;
            AppendSimulationInit(stringBuilder);
            AppendScenarioLoadMap(stringBuilder, scenarioManager.MapManager.CurrentMapName);
            var agents = scenarioManager.GetComponentsInChildren<ScenarioAgent>();
            foreach (var agent in agents)
            {
                switch (agent.Source.AgentTypeId)
                {
                    //Ego
                    case 1:
                        AppendScenarioAddAgent(stringBuilder, agent, "EGO");
                        break;
                    //NPC
                    case 2:
                        AppendScenarioAddAgent(stringBuilder, agent, "NPC");
                        AppendScenarioWaypoints(stringBuilder, agent);
                        break;
                    //Pedestrian
                    case 3:
                        AppendScenarioAddAgent(stringBuilder, agent, "PEDESTRIAN");
                        AppendScenarioWaypoints(stringBuilder, agent);
                        break;
                }
            }

            AppendSimulationStart(stringBuilder);

            return new PythonScenario(stringBuilder.ToString());
        }

        /// <summary>
        /// Appends a single line text to the string builder
        /// </summary>
        /// <param name="stringBuilder">String builder where the line will be appended</param>
        /// <param name="line">Text that will be appended in the line</param>
        private static void AppendLine(StringBuilder stringBuilder, string line)
        {
            for (int i = 0; i < indentLevel; i++)
                stringBuilder.Append("	");
            stringBuilder.Append(line);
            stringBuilder.Append("\n");
        }

        /// <summary>
        /// Appends the simulation initialization code to the string builder
        /// </summary>
        /// <param name="stringBuilder">String builder where the code will be appended</param>
        private static void AppendSimulationInit(StringBuilder stringBuilder)
        {
            AppendLine(stringBuilder, "#!/usr/bin/env python3");
            AppendLine(stringBuilder, "#");
            AppendLine(stringBuilder, "# Copyright (c) 2019 LG Electronics, Inc.");
            AppendLine(stringBuilder, "#");
            AppendLine(stringBuilder, "# This software contains code licensed as described in LICENSE.");
            AppendLine(stringBuilder, "#");
            AppendLine(stringBuilder, "");
            AppendLine(stringBuilder, "import os");
            AppendLine(stringBuilder, "import lgsvl");
            AppendLine(stringBuilder, "");
            AppendLine(stringBuilder,
                "sim = lgsvl.Simulator(os.environ.get(\"SIMULATOR_HOST\", \"127.0.0.1\"), 8181);");
        }

        /// <summary>
        /// Appends the simulation run code to the string builder
        /// </summary>
        /// <param name="stringBuilder">String builder where the code will be appended</param>
        private static void AppendSimulationStart(StringBuilder stringBuilder)
        {
            AppendLine(stringBuilder, "sim.run()");
        }

        /// <summary>
        /// Appends the load map code to the string builder
        /// </summary>
        /// <param name="stringBuilder">String builder where the code will be appended</param>
        /// <param name="mapName">Map name which will be loaded in this script</param>
        private static void AppendScenarioLoadMap(StringBuilder stringBuilder, string mapName)
        {
            AppendLine(stringBuilder, $"if sim.current_scene == \"{mapName}\":");
            indentLevel++;
            AppendLine(stringBuilder, "sim.reset();");
            indentLevel--;
            AppendLine(stringBuilder, "else:");
            indentLevel++;
            AppendLine(stringBuilder, $"sim.load(\"{mapName}\")");
            indentLevel--;
            AppendLine(stringBuilder, "");
        }

        /// <summary>
        /// Appends a scenario agent setup code to the string builder
        /// </summary>
        /// <param name="stringBuilder">String builder where the code will be appended</param>
        /// <param name="agent">Agent which will be serializec</param>
        /// <param name="agentType">Agent type name</param>
        private static void AppendScenarioAddAgent(StringBuilder stringBuilder, ScenarioAgent agent, string agentType)
        {
            var position = agent.TransformToMove.position;
            var rotation = agent.TransformToRotate.rotation.eulerAngles;
            AppendLine(stringBuilder, "state = lgsvl.AgentState()");
            AppendLine(stringBuilder, $"state.transform.position = lgsvl.Vector{position}");
            AppendLine(stringBuilder, $"state.transform.rotation = lgsvl.Vector{rotation}");
            AppendLine(stringBuilder,
                $"agent = sim.add_agent(\"{agent.Variant.name}\", lgsvl.AgentType.{agentType}, state)");
            AppendLine(stringBuilder, "");
        }

        /// <summary>
        /// Appends the waypoints setup code to the string builder
        /// </summary>
        /// <param name="stringBuilder">String builder where the code will be appended</param>
        /// <param name="agent">Agent for which waypoints are added</param>
        private static void AppendScenarioWaypoints(StringBuilder stringBuilder, ScenarioAgent agent)
        {
            AppendLine(stringBuilder, "waypoints = []");
            var angle = Vector3.zero;
            for (var i = 0; i < agent.Waypoints.Count; i++)
            {
                var waypoint = agent.Waypoints[i];
                var hasNextWaypoint = i + 1 < agent.Waypoints.Count;
                var nextWaypointPosition = hasNextWaypoint
                    ? agent.Waypoints[i + 1].transform.position
                    : Vector3.zero;
                var position = waypoint.transform.position;
                angle = hasNextWaypoint
                    ? Quaternion.LookRotation(nextWaypointPosition - position).eulerAngles
                    : angle;
                AppendLine(stringBuilder,
                    $"wp = lgsvl.DriveWaypoint(lgsvl.Vector{position}, {waypoint.Speed}, lgsvl.Vector{angle}, {waypoint.WaitTime})");
                AppendLine(stringBuilder, "waypoints.append(wp)");
            }

            AppendLine(stringBuilder, "agent.follow(waypoints)");
            AppendLine(stringBuilder, "");
        }
    }
}