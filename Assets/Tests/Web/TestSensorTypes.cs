/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Json;
using Nancy.Json.Simple;
using Nancy.Testing;
using NUnit.Framework;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using Simulator.Web;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Simulator.Tests.Web
{
    [SensorType("String", new[] { typeof(ImageData) })]
    public class StringSensor
    {
        [SensorParameter]
        public string Help = "helping";

        [SensorParameter]
        public string LG;
        
        int NotPresent;
    }

    [SensorType("Float", new[] { typeof(ImageData) })]
    public class FloatSensor
    {
        [SensorParameter]
        public float time = 150f;

        [SensorParameter]
        public float LG;
        
        int NotPresent;
    }

    [SensorType("Int", new[] { typeof(ImageData) })]
    public class IntSensor
    {
        [SensorParameter]
        public int year = 2019;

        [SensorParameter]
        public int LG;
        
        int NotPresent;
    }

    [SensorType("Bool", new[] { typeof(ImageData) })]
    public class BoolSensor
    {
        [SensorParameter]
        public bool happy = true;

        [SensorParameter]
        public bool duckie;
        
        int NotPresent;
    }

    [SensorType("Color", new[] { typeof(ImageData) })]
    public class ColorSensor
    {
        [SensorParameter]
        public Color picture = Color.magenta;

        [SensorParameter]
        public Color space;

        int NotPresent;

    }

    [SensorType("Enum", new [] { typeof(ImageData) })]
    public class EnumSensor
    {
        public enum Vehicle { A, B, C };

        [SensorParameter]
        public Vehicle car = Vehicle.C;

        [SensorParameter]
        public Vehicle truck;

        int NotPresent;
    }

    [SensorType("ManyData", new [] {
        typeof(ImageData), 
        typeof(PointCloudData), 
        typeof(Detected3DObjectArray), 
        typeof(Detected3DObjectData)
    })]
    public class ManyDataSensor
    {
        [SensorParameter]
        public int data = 2;
    }

    [SensorType("MinMax", new [] { typeof(ImageData) })]
    public class MinMaxSensor
    {
        [SensorParameter]
        [UnityEngine.Range(1, 1920)]
        public int length = 500;

        [SensorParameter]
        [UnityEngine.Range(0f, 9f)]
        public float distance = 5f;

        [SensorParameter]
        public bool sad = false;
    }

    public class IncompleteSensor
    {
        [SensorParameter]
        public bool irrelevant = true;
    }

    [SensorType("Double", new [] { typeof(ImageData) })]
    public class DoubleSensor
    {
        [SensorParameter]
        public double number = 15;
    }

    public class TestSensorTypes
    {
        Browser Browser;

        public TestSensorTypes()
        {
            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.RootPathProvider<UnityRootPathProvider>();
                    config.Module<SensorTypesModule>();
                }),
                ctx => {
                    ctx.Accept("application/json");
                    ctx.HttpRequest();
                }
            );
        }

        [Test]
        public void TestRequest()
        {
            var expectedSensors = RuntimeSettings.Instance.SensorPrefabs;
            var sensorNames = new List<string>();
            foreach (var sensor in expectedSensors)
            {
                var sensorType = sensor.GetType().GetCustomAttribute<SensorType>();
                sensorNames.Add(sensorType.Name);
            }
            Assert.AreNotEqual(0, sensorNames.Count);

            var result = Browser.Get("/sensor-types").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var js = new JavaScriptSerializer();
            var list = js.Deserialize<List<SensorConfig>>(result.Body.AsString());
            Assert.AreEqual(expectedSensors.Count, list.Count);

            for (int i = 0; i < expectedSensors.Count; i++)
            {
                var serialized = SimpleJson.SerializeObject(list[i]);
                var sensor = js.Deserialize<SensorConfig>(serialized);
                var idx  = sensorNames.FindIndex(elem => elem == sensor.Name);
                Assert.AreNotEqual(-1, idx, $"{sensor.Name} not found in expected sensor list");
                sensorNames.RemoveAt(idx);
            }

            Assert.AreEqual(0, sensorNames.Count);
        }

        [Test]
        public void TestStringSensor()
        {
            var sensor = new StringSensor();
            var parameterCount = 2;
            var typeCount = 1;

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("String", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("Help", config.parameters[0].Name);
            Assert.AreEqual("helping", config.parameters[0].DefaultValue);
            Assert.AreEqual("string", config.parameters[0].Type);
            Assert.AreEqual("LG", config.parameters[1].Name);
            Assert.AreEqual(null, config.parameters[1].DefaultValue);
            Assert.AreEqual("string", config.parameters[1].Type);
            Assert.False(config.parameters.Exists(p => p.Name == "NotPresent"));
        }

        [Test]
        public void TestFloatSensor()
        {
            var sensor = new FloatSensor();
            var parameterCount = 2;
            var typeCount = 1;

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("Float", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("time", config.parameters[0].Name);
            Assert.AreEqual(150f, config.parameters[0].DefaultValue);
            Assert.AreEqual("float", config.parameters[0].Type);
            Assert.AreEqual("LG", config.parameters[1].Name);
            Assert.AreEqual(0.0f, config.parameters[1].DefaultValue);
            Assert.AreEqual("float", config.parameters[1].Type);
            Assert.False(config.parameters.Exists(p => p.Name == "NotPresent"));
        }

        [Test]
        public void TestIntSensor()
        {
            var sensor = new IntSensor();
            var parameterCount = 2;
            var typeCount = 1;

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("Int", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("year", config.parameters[0].Name);
            Assert.AreEqual(2019, config.parameters[0].DefaultValue);
            Assert.AreEqual("int", config.parameters[0].Type);
            Assert.AreEqual("LG", config.parameters[1].Name);
            Assert.AreEqual(0, config.parameters[1].DefaultValue);
            Assert.AreEqual("int", config.parameters[1].Type);
            Assert.False(config.parameters.Exists(p => p.Name == "NotPresent"));
        }

        [Test]
        public void TestBoolSensor()
        {
            var sensor = new BoolSensor();
            var parameterCount = 2;
            var typeCount = 1;

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("Bool", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("happy", config.parameters[0].Name);
            Assert.AreEqual(true, config.parameters[0].DefaultValue);
            Assert.AreEqual("bool", config.parameters[0].Type);
            Assert.AreEqual("duckie", config.parameters[1].Name);
            Assert.AreEqual(false, config.parameters[1].DefaultValue);
            Assert.AreEqual("bool", config.parameters[1].Type);
            Assert.False(config.parameters.Exists(p => p.Name == "NotPresent"));
        }

        [Test]
        public void TestColorSensor()
        {
            var sensor = new ColorSensor();
            var parameterCount = 2;
            var typeCount = 1;

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("Color", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("picture", config.parameters[0].Name);
            Assert.AreEqual(SensorTypes.ColorValue((Color)Color.magenta), config.parameters[0].DefaultValue);
            Assert.AreEqual("color", config.parameters[0].Type);
            Assert.AreEqual("space", config.parameters[1].Name);
            Assert.AreEqual("#00000000", config.parameters[1].DefaultValue);
            Assert.AreEqual("color", config.parameters[1].Type);
            Assert.False(config.parameters.Exists(p => p.Name == "NotPresent"));
        }

        [Test]
        public void TestEnumSensor()
        {
            var sensor  = new EnumSensor();
            var parameterCount = 2;
            var typeCount = 1;
            var valuesCount = 3;

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("Enum", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("car", config.parameters[0].Name);
            Assert.AreEqual("C", config.parameters[0].DefaultValue);
            Assert.AreEqual("enum", config.parameters[0].Type);
            Assert.AreEqual("truck", config.parameters[1].Name);
            Assert.AreEqual("A", config.parameters[1].DefaultValue);
            Assert.AreEqual("enum", config.parameters[1].Type);
            Assert.AreEqual(valuesCount, config.parameters[0].Values.Length);
            Assert.AreEqual("A", config.parameters[0].Values[0]);
            Assert.AreEqual("B", config.parameters[0].Values[1]);
            Assert.AreEqual("C", config.parameters[0].Values[2]);
            Assert.False(config.parameters.Exists(p => p.Name == "NotPresent"));
        }

        [Test]
        public void TestManyDataSensor()
        {
            var sensor = new ManyDataSensor();
            var parameterCount = 1;
            var expectedTypes = new []
            {
                typeof(ImageData),
                typeof(PointCloudData),
                typeof(Detected3DObjectArray),
                typeof(Detected3DObjectData),
            };

            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("ManyData", config.Name);
            Assert.AreEqual(expectedTypes.Length, config.Types.Length);
            for (int i=0; i<expectedTypes.Length; i++)
            {
                Assert.AreEqual(expectedTypes[i].ToString(), config.Types[i], $"Wrong type, expected {expectedTypes[i]}, got {config.Types[i]}");
            }
            Assert.AreEqual(parameterCount, config.parameters.Count);
            Assert.AreEqual("data", config.parameters[0].Name);
            Assert.AreEqual(2, config.parameters[0].DefaultValue);
        }

        [Test]
        public void TestMinMaxSensor()
        {
            var sensor = new MinMaxSensor();
            var parameterCount = 3;
            var typeCount = 1;
            
            var config = SensorTypes.GetConfig(sensor);

            Assert.AreEqual("MinMax", config.Name);
            Assert.AreEqual(typeCount, config.Types.Length);
            Assert.AreEqual("Simulator.Bridge.Data.ImageData", config.Types[0]);
            Assert.AreEqual(parameterCount, config.parameters.Count);

            Assert.AreEqual("length", config.parameters[0].Name);
            Assert.AreEqual(500, config.parameters[0].DefaultValue);
            Assert.AreEqual(1f, config.parameters[0].Min);
            Assert.AreEqual(1920f, config.parameters[0].Max);

            Assert.AreEqual("distance", config.parameters[1].Name);
            Assert.AreEqual(5f, config.parameters[1].DefaultValue);
            Assert.AreEqual(0f, config.parameters[1].Min);
            Assert.AreEqual(9f, config.parameters[1].Max);

            Assert.AreEqual("sad", config.parameters[2].Name);
            Assert.AreEqual(false, config.parameters[2].DefaultValue);
            Assert.AreEqual(null, config.parameters[2].Min);
            Assert.AreEqual(null, config.parameters[2].Max);
        }

        [Test]
        public void TestIncompleteSensor()
        {
            var sensor  = new IncompleteSensor();

            var ex = Assert.Throws<Exception>(() => SensorTypes.GetConfig(sensor));
            Assert.True(ex.Message.Contains("Sensor Configuration Error: Simulator.Tests.Web.IncompleteSensor"));
        }

        [Test]
        public void TestDoubleSensor()
        {
            var sensor = new DoubleSensor();

            var ex = Assert.Throws<Exception>(() => SensorTypes.GetConfig(sensor));
            Assert.True(ex.Message.Contains("Sensor Configuration Error: Simulator.Tests.Web.DoubleSensor"));
        }
    }
}