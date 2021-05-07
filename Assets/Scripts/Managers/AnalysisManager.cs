/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simulator.Sensors;
using System.IO;
using System.Net;
using System.Collections;
using System.Threading.Tasks;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using System.Reflection;
using System.Linq;
using Simulator.Utilities;
using System.Text.RegularExpressions;

namespace Simulator.Analysis
{

    public class AnalysisManager : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        public enum AnalysisStatusType
        {
            InProgress,
            Success,
            Failed,
            Error,
        };
        private AnalysisStatusType Status = AnalysisStatusType.Error;

        private JsonSerializerSettings SerializerSettings;
        private SimulationConfig SimConfig;
        private List<SensorBase> Sensors = new List<SensorBase>();
        private List<Hashtable> AnalysisEvents = new List<Hashtable>();
        private JArray Results = new JArray();

        private bool Init = false;

        public struct CollisionTotalData
        {
            public int Ego;
            public int Npc;
            public int Ped;
        }
        private CollisionTotalData CollisionTotals;
        private DateTime AnalysisStart;

        private string PersistantPath;
        private string AnalysisPath;
        private string SimulationPath;

        #region network
        public string Key { get; } = "AnalysisManager";

        private int ReceivedResponses = 0;

        private Dictionary<uint, JObject> ClientsSensorsData = new Dictionary<uint, JObject>();
        #endregion

        private void Awake()
        {
            // TODO create from loader, remove from simulationmanager

            SerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            PersistantPath = Simulator.Web.Config.PersistentDataPath;
            AnalysisPath = Path.Combine(PersistantPath, "Analysis");
            if (!Directory.Exists(AnalysisPath))
            {
                Directory.CreateDirectory(AnalysisPath);
            }
        }

        private void Start()
        {
            if (!SimulatorManager.Instance.IsAPI)
            {
                AnalysisInit();
            }
            Loader.Instance.Network.MessagesManager?.RegisterObject(this);
        }

        private void OnDestroy()
        {
            var nonBlockingTask = Deinitialize();
        }

        private async Task Deinitialize()
        {
            await AnalysisSave();
            AnalysisSend();

            if (Directory.Exists(AnalysisPath))
            {
                Directory.Delete(AnalysisPath);
            }

            Loader.Instance.Network.MessagesManager?.UnregisterObject(this);
        }

        private void FixedUpdate()
        {
            if (SimConfig == null)// || !SimConfig.CurrentTestId.HasValue) // This will need to taken from WISE or sent to simconfig, then read
            {
                return; // Development mode or generate report false
            }

            foreach (var sensor in Sensors)
            {
                sensor.OnAnalyze();
            }
        }

        public void AnalysisInit()
        {
            if (Init)
            {
                return;
            }
            SimConfig = Loader.Instance?.SimConfig;

            Console.WriteLine("[ANMGR] Initializing with TestReportId:{0}", (SimConfig?.TestReportId is null) ? "<null>" : SimConfig.TestReportId);

            Sensors.Clear();
            AnalysisEvents.Clear();
            CollisionTotals.Ego = 0;
            CollisionTotals.Npc = 0;
            CollisionTotals.Ped = 0;
            ReceivedResponses = 0;
            ClientsSensorsData.Clear();
            Status = AnalysisStatusType.InProgress;
            AnalysisStart = DateTime.Now;

            if (SimConfig == null)// || !SimConfig.CurrentTestId.HasValue) // This will need to taken from WISE or sent to simconfig, then read
            {
                return; // Development mode or generate report false
            }

            if (SimulatorManager.Instance.IsAPI)
            {
                SimConfig.Agents = SimulatorManager.Instance.AgentManager.ActiveAgents.ToArray();
            }

            foreach (var agent in SimConfig.Agents)
            {
                if (agent.AgentGO == null)
                    continue;
                
                Array.ForEach(agent.AgentGO.GetComponentsInChildren<SensorBase>(), sensorBase =>
                {
                    Sensors.Add(sensorBase);

                    var sensorType = sensorBase.GetType().GetCustomAttribute<SensorType>();
                    if (sensorType.Name == "Video Recording")
                    {
                        sensorBase.GetType().GetMethod("StartRecording").Invoke(sensorBase, new object[] { null });
                    }
                });
            }
            Init = true;
        }

        public async Task AnalysisSave()
        {
            if (!Init)
            {
                return;
            }

            SimConfig = Loader.Instance?.SimConfig;
            if (SimConfig == null)// || !SimConfig.CurrentTestId.HasValue) // This will need to taken from WISE or sent to simconfig, then read
            {
                return; // Development mode or generate report false
            }

            if (Status != AnalysisStatusType.InProgress)
            {
                return;
            }

            JArray agentsJA = new JArray();
            //If it is a distributed simulation, gather all sensors data in the master node
            if (Loader.Instance != null)
            {
                if (Loader.Instance.Network.IsMaster)
                {
                    var clientsCount = Loader.Instance.Network.ClientsCount;
                    var timeout = Loader.Instance.Network.Settings.Timeout / 1000;
                    var startTime = Time.unscaledTime;
                    while (ReceivedResponses < clientsCount &&
                           Loader.Instance.Network.Master.Clients.Count > 0 &&
                           Time.unscaledTime - startTime < timeout)
                        await Task.Delay(100);
                    if (ReceivedResponses < clientsCount)
                        Debug.LogWarning($"{GetType().Name} received {ReceivedResponses} analysis reports from {clientsCount} clients.");
                }
                else if (Loader.Instance.Network.IsClient)
                {
                    //Gather all the sensors data of all the agents
                    foreach (var agent in SimConfig.Agents)
                    {
                        JObject agentJO = new JObject();
                        agentJO.Add("Name", agent.Name);
                        agentJO.Add("id", agent.GTID);
                        JArray sensorsJO = GetSensorsData(agent, agentJO);
                        agentJO.Add("Sensors", sensorsJO);
                        agentsJA.Add(agentJO);
                    }

                    var dataString = agentsJA.ToString();
                    var message = MessagesPool.Instance.GetMessage(BytesStack.GetMaxByteCount(dataString));
                    message.AddressKey = Key;
                    message.Content.PushString(dataString);
                    message.Type = DistributedMessageType.ReliableOrdered;
                    //Send all collected data to the master
                    BroadcastMessage(message);
                    
                    var timeout = Loader.Instance.Network.Settings.Timeout / 1000;
                    var startTime = Time.unscaledTime;
                    //Wait for the master response
                    while (ReceivedResponses < 1 &&
                           Loader.Instance.Network.Client.IsConnected &&
                           Time.unscaledTime - startTime < timeout)
                        await Task.Delay(100);
                    Init = false;
                    return;
                }
            }

            var dt = string.Format("Analysis_{0:yyyy-MM-dd_hh-mm-sstt}", DateTime.Now);
            var simName = (new Regex(@"[<>:""/\|?*]")).Replace(SimConfig.Name, "");
            simName = simName.Replace(@"\", "");
            SimulationPath = Path.Combine(AnalysisPath, simName, dt);
            if (!Directory.Exists(SimulationPath))
                Directory.CreateDirectory(SimulationPath);

            SetStatus();

            var simulationConfigJS = JsonConvert.SerializeObject(SimConfig, SerializerSettings);
            var resultRoot = new JObject();
            var configJO = JObject.Parse(simulationConfigJS);
            var agents = (JArray)configJO["Agents"];
            foreach (var agent in agents)
            {
                var agentObj = agent as JObject;
                if(!string.IsNullOrEmpty(agent["Sensors"].ToString()))
                {
                    JToken token = JToken.Parse(agent["Sensors"].ToString());
                    agentObj["Sensors"] = token;
                }
                else
                {
                    agentObj["Sensors"] = new JArray();
                }
            }

            //JObject resultsJO = new JObject();
            foreach (var agent in SimConfig.Agents)
            {
                JObject agentJO = new JObject();
                agentJO.Add("Name", agent.Name);
                agentJO.Add("id", agent.GTID);
                JArray sensorsJO = GetSensorsData(agent, agentJO);

                //Add sensors data from clients
                if (Loader.Instance != null && Loader.Instance.Network.IsMaster)
                {
                    if (ClientsSensorsData.TryGetValue(agent.GTID, out var agentSensors))
                        foreach (var clientSensorData in agentSensors.Properties())
                            sensorsJO.Add(clientSensorData.Value);
                }
                agentJO.Add("Sensors", sensorsJO);

                JArray eventsJA = new JArray();
                foreach (var hashtable in AnalysisEvents)
                {
                    if (hashtable.ContainsKey("Id"))
                    {
                        if ((uint)hashtable["Id"] == agent.GTID)
                        {
                            var sData = JsonConvert.SerializeObject(hashtable, SerializerSettings);
                            JToken token = JToken.Parse(sData);
                            eventsJA.Add(token);
                        }
                    }
                }
                agentJO.Add("Events", eventsJA);
                agentsJA.Add(agentJO);
            }

            JObject iterationInfo = new JObject();
            iterationInfo.Add("Duration", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString());
            iterationInfo.Add("StartTime", AnalysisStart);
            iterationInfo.Add("StopTime", DateTime.Now);
            iterationInfo.Add("Version", "0.2");
            iterationInfo.Add("Status", Status.ToString());
            if (Status == AnalysisStatusType.Error)
            {
                var reasons = AnalysisEvents.Where(e => e.ContainsKey("Error") && e.ContainsKey("Status")).Select(e => e["Error"]);
                iterationInfo.Add("ErrorReason", String.Join("\n", reasons));
            }

            resultRoot.Add("Agents", agentsJA);
            resultRoot.Add("simulationConfig", configJO);
            resultRoot.Add("IterationInfo", iterationInfo);

            Results.Add(resultRoot);
            Init = false;
        }

        private JArray GetSensorsData(AgentConfig agent, JObject agentJO)
        {
            var sensorsJO = new JArray();
            foreach (var sensorBase in agent.AgentGO.GetComponentsInChildren<SensorBase>())
            {
                sensorBase.SetAnalysisData();
                List<AnalysisReportItem> report = sensorBase.SensorAnalysisData;
                if(report == null)
                {
                    report = new List<AnalysisReportItem>();
                }

                foreach (var info in sensorBase.GetType().GetRuntimeProperties().Where(prop => prop.IsDefined(typeof(AnalysisMeasurement), true)))
                {
                    var attr = info.GetCustomAttribute<AnalysisMeasurement>();
                    var measurement = new AnalysisReportItem
                    {
                        name = String.IsNullOrEmpty(attr.Name) ? info.Name : attr.Name,
                        type = attr.Type,
                        value = info.GetValue(sensorBase)
                    };
                    report.Add(measurement);
                }

                foreach (var info in sensorBase.GetType().GetRuntimeFields().Where(field => field.IsDefined(typeof(AnalysisMeasurement), true)))
                {
                    var attr = info.GetCustomAttribute<AnalysisMeasurement>();
                    var measurement = new AnalysisReportItem
                    {
                        name = String.IsNullOrEmpty(attr.Name) ? info.Name : attr.Name,
                        type = attr.Type,
                        value = info.GetValue(sensorBase)
                    };
                    report.Add(measurement);
                }

                var sData = JsonConvert.SerializeObject(new
                {
                    items = report,
                    Type = sensorBase.GetType().ToString()
                }, SerializerSettings);
                JToken token = JToken.Parse(sData);
                sensorsJO.Add(token);
            }

            return sensorsJO;
        }


        private void AnalysisSend()
        {
            if (SimConfig == null)// || !SimConfig.CurrentTestId.HasValue) // This will need to taken from WISE or sent to simconfig, then read
            {
                return; // Development mode or generate report false
            }

            if (Status == AnalysisStatusType.InProgress)
            {
                return;
            }

            File.WriteAllText(Path.Combine(SimulationPath, "SimulationConfiguration.json"), Results.ToString(Formatting.Indented));

            if (!string.IsNullOrEmpty(SimConfig.TestReportId))
            {
                Console.WriteLine("[ANMGR] Sending test report data id:{0}", SimConfig.TestReportId);
                ConnectionManager.API.SendAnalysis<JArray>(SimConfig.TestReportId, Results);
            }
            else
            {
                Console.WriteLine("[ANMGR] Skip sending report: TestReportId is null");
                Debug.LogWarning("AnalysisSend faild due to null TestReportId");
            }
        }

        public void AddEvent(Hashtable data)
        {
            AnalysisEvents.Add(data);
        }

        public void IncrementEgoCollision(uint id, Vector3 location, Vector3 egoVelocity, Vector3 otherVelocity, string otherType)
        {
            CollisionTotals.Ego++;
            var data = new Hashtable
            {
                { "Id", id },
                { "Type", "EgoCollision" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Location", location },
                { "OtherType", otherType },
                { "EgoVelocity", egoVelocity },
                { "OtherVelocity", otherVelocity },
                { "EgoCollisionTotal", CollisionTotals.Ego },
                { "Status", AnalysisStatusType.Failed },
            };
            AddEvent(data);
        }

        public void IncrementNPCCollision()
        {
            CollisionTotals.Npc++;
        }

        public void IncrementPedCollision()
        {
            CollisionTotals.Ped++;
        }

        public void AddErrorEvent(string error)
        {
            var data = new Hashtable
            {
                { "Error", error},
                { "Status", AnalysisStatusType.Error },
            };
            AddEvent(data);
        }

        private void SetStatus()
        {
            Status = AnalysisStatusType.Success;
            foreach (var hashtable in AnalysisEvents)
            {
                if (hashtable.ContainsValue(AnalysisStatusType.Failed))
                {
                    Status = AnalysisStatusType.Failed;
                }

                // Error always overwrites test case fail
                if (hashtable.ContainsValue(AnalysisStatusType.Error))
                {
                    Status = AnalysisStatusType.Error;
                    break;
                }
            }

            // cycle every status to convert to string for json
            foreach (var hashtable in AnalysisEvents)
            {
                if (hashtable.ContainsKey("Status"))
                {
                    hashtable["Status"] = hashtable["Status"].ToString();
                }
            }
        }

        #region network
        void IMessageReceiver.ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            var network = Loader.Instance.Network;
            if (network.IsMaster)
            {
                var agentsString = distributedMessage.Content.PopString();
                var agentsData = JArray.Parse(agentsString);
                foreach (var agentDataToken in agentsData.Children())
                {
                    var agentData = agentDataToken as JObject;
                    if (agentData == null)
                        continue;
                    var agentId = agentData["id"].Value<uint>();
                    var sensorsJson = agentData["Sensors"] as JObject;
                    if (sensorsJson == null) continue;
                    if (ClientsSensorsData.TryGetValue(agentId, out var sensors))
                    {
                        foreach (var clientSensorData in sensorsJson.Properties())
                            sensors.Add(clientSensorData.Name, clientSensorData.Value);
                    }
                    else
                    {
                        ClientsSensorsData.Add(agentId, sensorsJson);
                    }
                }
                    
                //Send approve response to the client
                var message = MessagesPool.Instance.GetMessage();
                message.AddressKey = Key;
                message.Type = DistributedMessageType.ReliableOrdered;
                UnicastMessage(sender.PeerEndPoint, message);
                ReceivedResponses++;
            }
            else
            {
                //Register received response from the master
                ReceivedResponses++;
            }
        }

        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, distributedMessage);
        }

        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.BroadcastMessage(distributedMessage);
        }

        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
            //Nothing to send
        }
        #endregion
    }

    public struct AnalysisReportItem
    {
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter), true)]
        public MeasurementType type;
        public string name;
        public object value;
    }
}
