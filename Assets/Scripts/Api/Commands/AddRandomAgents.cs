/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulator.Api.Commands
{
    class AddRandomAgents : IDistributedCommand
    {
        public string Name => "simulator/add_random_agents";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            if (SimulatorManager.Instance == null)
            {
                api.SendError(this, "SimulatorManager not found! Is scene loaded?");
                return;
            }

            var agentType = (AgentType)args["type"].AsInt;
            switch (agentType)
            {
                case AgentType.Npc:
                    var npcManager = SimulatorManager.Instance.NPCManager;
                    var pooledNPCs = npcManager.SpawnNPCPool();
                    npcManager.NPCActive = true;
                    npcManager.SetNPCOnMap();
                    api.SendResult(this);
                    break;
                case AgentType.Pedestrian:
                    var pedManager = SimulatorManager.Instance.PedestrianManager;
                    var pooledPeds = pedManager.SpawnPedPool();
                    pedManager.PedestriansActive = true;
                    pedManager.SetPedOnMap();
                    api.SendResult(this);
                    break;
                default:
                    api.SendError(this, $"Unsupported '{args["type"]}' type");
                    break;
            }
        }

        
    }
}
