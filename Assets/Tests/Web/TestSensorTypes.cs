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
            var sensors = js.Deserialize<List<SensorConfig>>(result.Body.AsString());
            Assert.AreEqual(expectedSensors.Count, sensors.Count);

            foreach (var sensor in sensors)
            {
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("Help", config.Parameters[0].Name);
            Assert.AreEqual("helping", config.Parameters[0].DefaultValue);
            Assert.AreEqual("string", config.Parameters[0].Type);
            Assert.AreEqual("LG", config.Parameters[1].Name);
            Assert.AreEqual(null, config.Parameters[1].DefaultValue);
            Assert.AreEqual("string", config.Parameters[1].Type);
            Assert.False(config.Parameters.Exists(p => p.Name == "NotPresent"));
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("time", config.Parameters[0].Name);
            Assert.AreEqual(150f, config.Parameters[0].DefaultValue);
            Assert.AreEqual("float", config.Parameters[0].Type);
            Assert.AreEqual("LG", config.Parameters[1].Name);
            Assert.AreEqual(0.0f, config.Parameters[1].DefaultValue);
            Assert.AreEqual("float", config.Parameters[1].Type);
            Assert.False(config.Parameters.Exists(p => p.Name == "NotPresent"));
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("year", config.Parameters[0].Name);
            Assert.AreEqual(2019, config.Parameters[0].DefaultValue);
            Assert.AreEqual("int", config.Parameters[0].Type);
            Assert.AreEqual("LG", config.Parameters[1].Name);
            Assert.AreEqual(0, config.Parameters[1].DefaultValue);
            Assert.AreEqual("int", config.Parameters[1].Type);
            Assert.False(config.Parameters.Exists(p => p.Name == "NotPresent"));
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("happy", config.Parameters[0].Name);
            Assert.AreEqual(true, config.Parameters[0].DefaultValue);
            Assert.AreEqual("bool", config.Parameters[0].Type);
            Assert.AreEqual("duckie", config.Parameters[1].Name);
            Assert.AreEqual(false, config.Parameters[1].DefaultValue);
            Assert.AreEqual("bool", config.Parameters[1].Type);
            Assert.False(config.Parameters.Exists(p => p.Name == "NotPresent"));
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("picture", config.Parameters[0].Name);
            Assert.AreEqual("#FF00FFFF", config.Parameters[0].DefaultValue);
            Assert.AreEqual("color", config.Parameters[0].Type);
            Assert.AreEqual("space", config.Parameters[1].Name);
            Assert.AreEqual("#00000000", config.Parameters[1].DefaultValue);
            Assert.AreEqual("color", config.Parameters[1].Type);
            Assert.False(config.Parameters.Exists(p => p.Name == "NotPresent"));
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("car", config.Parameters[0].Name);
            Assert.AreEqual("C", config.Parameters[0].DefaultValue);
            Assert.AreEqual("enum", config.Parameters[0].Type);
            Assert.AreEqual("truck", config.Parameters[1].Name);
            Assert.AreEqual("A", config.Parameters[1].DefaultValue);
            Assert.AreEqual("enum", config.Parameters[1].Type);
            Assert.AreEqual(valuesCount, config.Parameters[0].Values.Length);
            Assert.AreEqual("A", config.Parameters[0].Values[0]);
            Assert.AreEqual("B", config.Parameters[0].Values[1]);
            Assert.AreEqual("C", config.Parameters[0].Values[2]);
            Assert.False(config.Parameters.Exists(p => p.Name == "NotPresent"));
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);
            Assert.AreEqual("data", config.Parameters[0].Name);
            Assert.AreEqual(2, config.Parameters[0].DefaultValue);
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
            Assert.AreEqual(parameterCount, config.Parameters.Count);

            Assert.AreEqual("length", config.Parameters[0].Name);
            Assert.AreEqual(500, config.Parameters[0].DefaultValue);
            Assert.AreEqual(1f, config.Parameters[0].Min);
            Assert.AreEqual(1920f, config.Parameters[0].Max);

            Assert.AreEqual("distance", config.Parameters[1].Name);
            Assert.AreEqual(5f, config.Parameters[1].DefaultValue);
            Assert.AreEqual(0f, config.Parameters[1].Min);
            Assert.AreEqual(9f, config.Parameters[1].Max);

            Assert.AreEqual("sad", config.Parameters[2].Name);
            Assert.AreEqual(false, config.Parameters[2].DefaultValue);
            Assert.AreEqual(null, config.Parameters[2].Min);
            Assert.AreEqual(null, config.Parameters[2].Max);
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