/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Api.Commands
{
    class LoadScene : ICommand
    {
        public string Name { get { return "simulator/load_scene"; } }

        static void DoLoad(string client, string name)
        {
            Time.timeScale = 0;
            ApiManager.Instance.TimeLimit = 0.0;
            ApiManager.Instance.FrameLimit = 0;

            ApiManager.Instance.Agents.Clear();
            ApiManager.Instance.Sensors.Clear();
            ApiManager.Instance.SensorUID.Clear();

            NPCManager.Instance?.DespawnAllNPC();

            var menu = Object.FindObjectOfType<MenuManager>();
            menu.LoadScene(name, () =>
            {
                var parkedCars = GameObject.Find("ParkedCarHolder");
                parkedCars?.SetActive(false);

                ApiManager.Instance.CurrentScene = name;
                ApiManager.Instance.CurrentTime = 0.0;
                ApiManager.Instance.CurrentFrame = 0;
                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            });
        }

        public void Execute(string client, JSONNode args)
        {
            var name = args["scene"].Value;

            var menu = Object.FindObjectOfType<MenuManager>();
            if (menu == null)
            {
                foreach (var kv in ApiManager.Instance.Agents)
                {
                    var obj = kv.Value;
                    var setup = obj.GetComponent<AgentSetup>();
                    if (setup != null)
                    {
                        var sensors = setup.GetSensors();
                        foreach (var sensor in sensors)
                        {
                            var suid = ApiManager.Instance.SensorUID[sensor];
                            ApiManager.Instance.Sensors.Remove(suid);
                            ApiManager.Instance.SensorUID.Remove(sensor);
                        }

                        SimulatorManager.Instance.DespawnVehicle(setup.Connector);
                        ROSAgentManager.Instance.Remove(obj);
                        Object.Destroy(obj);
                    }

                    var npc = obj.GetComponent<NPCControllerComponent>();
                    if (npc != null)
                    {
                        NPCManager.Instance.DespawnVehicle(obj);
                    }
                }

                ApiManager.Instance.Sensors.Clear();
                ApiManager.Instance.SensorUID.Clear();
                ApiManager.Instance.Agents.Clear();

                var loader = SceneManager.LoadSceneAsync("Menu");
                loader.completed += op => DoLoad(client, name);
            }
            else
            {
                DoLoad(client, name);
            }
        }
    }
}
