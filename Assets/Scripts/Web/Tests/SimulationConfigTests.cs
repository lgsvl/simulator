/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Web.Tests
{
    using NUnit.Framework;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using UnityEngine;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Collections.Generic;

    [TestFixture]
    class ParseSimulationConfig
    {
        private JObject loadJsonFile(string filename)
        {
            string path = Path.Combine(Application.dataPath,
                                       "Scripts/Web/Tests/TestData",
                                       "SimulationConfig",
                                       filename);

            var jsonString = File.ReadAllText(path);
            return JObject.Parse(jsonString);
        }

        [Test]
        public void ParseApiOnly()
        {
            var json = loadJsonFile("apiOnly.json");

            var simData = json.ToObject<SimulationData>();
            SimulationConfigUtils.ProcessKnownTemplates(ref simData);

            Assert.That(simData.ApiOnly, Is.True);
            Assert.True(simData.ApiOnly);

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(simData);
            Debug.Log($"Simulation data: {s}");
        }

        [Test]
        public void ParseRandomTrafic()
        {
            var json = loadJsonFile("randomTraffic.json");

            var simData = json.ToObject<SimulationData>();
            SimulationConfigUtils.ProcessKnownTemplates(ref simData);

            Assert.That(simData.Template != null, Is.True);
            Assert.That(simData.Template.Alias, Is.EqualTo("randomTraffic"));
            Assert.That(simData.Template.Parameters.Length, Is.EqualTo(3));

            Assert.That(simData.ApiOnly, Is.False);

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(simData);
            Debug.Log($"Simulation data: {s}");

            Assert.That(simData.Map != null, Is.True);
            Assert.That(simData.Map.Id, Is.EqualTo("841b956b-7848-4c04-a017-5bd4d8385bbd"));
            Assert.That(simData.Vehicles != null, Is.True);
            Assert.That(simData.Vehicles[0].Id, Is.EqualTo("a65e1504-27d9-4338-8161-e59cef6de2bb"));
        }

        [Test]
        public void ParseRandomTrafficWithoutVehicles()
        {
            // Config data does not contain SIMULATOR_MAP parameter
            var json = loadJsonFile("randomTrafficWithoutVehicles.json");

            var simData = json.ToObject<SimulationData>();

            Assert.That( 
                () => { SimulationConfigUtils.ProcessKnownTemplates(ref simData); },
                Throws.InstanceOf<MissedTemplateParameterError>().With.Message.EqualTo("SIMULATOR_VEHICLES")
            );
        }

        [Test]
        public void ParseRandomTrafficWithoutMap()
        {
            // Config data does not contain SIMULATOR_MAP parameter

            var json = loadJsonFile("randomTrafficWithoutMap.json");

            var simData = json.ToObject<SimulationData>();

            Assert.That( 
                () => { SimulationConfigUtils.ProcessKnownTemplates(ref simData); },
                Throws.InstanceOf<MissedTemplateParameterError>().With.Message.EqualTo("SIMULATOR_MAP")
            );
        }

        [Test]
        public void ParseRandomTrafficFull()
        {
            var json = loadJsonFile("randomTrafficFull.json");

            var simData = json.ToObject<SimulationData>();
            SimulationConfigUtils.ProcessKnownTemplates(ref simData);

            Assert.That(simData.Template != null, Is.True);
            Assert.That(simData.Template.Alias, Is.EqualTo("randomTraffic"));
            Assert.That(simData.Template.Parameters.Length, Is.EqualTo(12));

            Assert.That(simData.ApiOnly, Is.False);

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(simData);
            Debug.Log($"Simulation data: {s}");

            Assert.That(simData.Map != null, Is.True);
            Assert.That(simData.Map.Id, Is.EqualTo("a7b32bdb-acc3-40e9-b893-29e4955a209f"));
            Assert.That(simData.Vehicles != null, Is.True);
            Assert.That(simData.Vehicles.Length, Is.EqualTo(1));
            Assert.That(simData.Vehicles[0].Id, Is.EqualTo("a88495aa-e4f9-4e1f-a790-cdbca41deeff"));
            Assert.That(simData.Seed, Is.EqualTo(42));
        }

        [Test]
        public void ParsePythonApi()
        {
            var json = loadJsonFile("pythonApi.json");

            var simData = json.ToObject<SimulationData>();
            SimulationConfigUtils.ProcessKnownTemplates(ref simData);

            Assert.That(simData.Template != null, Is.True);
            Assert.That(simData.Template.Alias, Is.EqualTo("pythonApi"));
            Assert.That(simData.Template.Parameters.Length, Is.EqualTo(3));

            Assert.That(simData.ApiOnly, Is.True);

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(simData);
            Debug.Log($"Simulation data: {s}");

            Assert.That(simData.Map == null, Is.True);
            Assert.That(simData.Vehicles == null, Is.True);

            var environment = new Dictionary<string, string>();
            SimulationConfigUtils.UpdateTestCaseEnvironment(simData.Template, environment);

            Assert.That(environment["SIMULATOR_API_ONLY"], Is.EqualTo("1"));
            Assert.That(environment["SIMULATOR_TC_FILENAME"], Is.EqualTo("scenario.py"));
        }

        [Test]
        public void ParseLeagcy01()
        {
            var json = loadJsonFile("legacy01.json");

            var simData = json.ToObject<SimulationData>();
            SimulationConfigUtils.ProcessKnownTemplates(ref simData);

            Assert.That(simData.Map != null, Is.True);
            Assert.That(simData.Map.Id, Is.EqualTo("841b956b-7848-4c04-a017-5bd4d8385bbd"));
            Assert.That(simData.Vehicles != null, Is.True);
            Assert.That(simData.Vehicles[0].Id, Is.EqualTo("a65e1504-27d9-4338-8161-e59cef6de2bb"));
       }
    }
}