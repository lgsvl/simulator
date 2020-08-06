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
using System.Collections;
using Simulator.Database.Services;
using System.IO;

namespace Simulator.Analysis
{
    public class AnalysisManager : MonoBehaviour
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
        public string TestReportId;

        private void Awake()
        {
            // TODO create from loader, remove from simulationmanager

            SerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };

            PersistantPath = Application.persistentDataPath;
            AnalysisPath = Path.Combine(PersistantPath, "Analysis");
            if (!Directory.Exists(AnalysisPath))
                Directory.CreateDirectory(AnalysisPath);
        }

        private void Start()
        {
            if (!SimulatorManager.Instance.IsAPI)
            {
                AnalysisInit();
            }
        }

        private void OnDestroy()
        {
            AnalysisSave();
            AnalysisSend();
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

            Console.WriteLine("[ANMGR] Initializing with TestReportId:{0}", (TestReportId is null) ? "<null>" : TestReportId);

            Sensors.Clear();
            AnalysisEvents.Clear();
            CollisionTotals.Ego = 0;
            CollisionTotals.Npc = 0;
            CollisionTotals.Ped = 0;
            Status = AnalysisStatusType.InProgress;
            AnalysisStart = DateTime.Now;

            SimConfig = Loader.Instance?.SimConfig;
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
                Array.ForEach(agent.AgentGO.GetComponentsInChildren<SensorBase>(), sensorBase =>
                {
                    Sensors.Add(sensorBase);

                    if (sensorBase is VideoRecordingSensor recorder)
                    {
                        recorder.StartRecording();
                    }
                });
            }
            Init = true;
        }

        public void AnalysisSave()
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

            var dt = string.Format("Analysis_{0:yyyy-MM-dd_hh-mm-sstt}", DateTime.Now);
            SimulationPath = Path.Combine(AnalysisPath, SimConfig.Name, dt);
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
                JToken token = JToken.Parse(agent["Sensors"].ToString());
                agentObj["Sensors"] = token;
            }

            //JObject resultsJO = new JObject();
            JArray agentsJA = new JArray();
            foreach (var agent in SimConfig.Agents)
            {
                JObject agentJO = new JObject();
                agentJO.Add("Name", agent.Name);
                agentJO.Add("id", agent.GTID);
                JObject sensorsJO = new JObject();
                Array.ForEach(agent.AgentGO.GetComponentsInChildren<SensorBase>(), sensorBase =>
                {
                    sensorBase.SetAnalysisData();
                    if (sensorBase.SensorAnalysisData != null)
                    {
                        var sData = JsonConvert.SerializeObject(sensorBase.SensorAnalysisData, SerializerSettings);
                        JToken token = JToken.Parse(sData);
                        sensorsJO.Add(sensorBase.GetType().ToString(), token);
                    }

                    if (sensorBase is VideoRecordingSensor recorder)
                    {
                        if (recorder.StopRecording())
                        {
                            agentJO.Add("VideoCapture",
                                        Path.Combine(recorder.GetOutdir(), recorder.GetFileName()));
                        }
                    }
                });
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
            iterationInfo.Add("Version", "0.1");

            resultRoot.Add("Agents", agentsJA);
            resultRoot.Add("simulationConfig", configJO);
            resultRoot.Add("IterationInfo", iterationInfo);

            Results.Add(resultRoot);
            Init = false;
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

            if (TestReportId != null)
            {
                Console.WriteLine("[ANMGR] Sending test report data id:{0}", TestReportId);
                ConnectionManager.API.SendAnalysis<JArray>(TestReportId, Results);
            }
            else
            {
                Console.WriteLine("[ANMGR] Skip sending report: TestReportId is null");
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
    }
}
