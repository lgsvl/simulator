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

    [TestFixture]
    class ParseTemplateParametersTest
    {
        [Test]
        public void ParseBoolTrueParameter()
        {
            string json = @"
                {
                    'alias': 'api-only',
                    'parameterName': 'Start Simulator API',
                    'parameterType': 'bool',
                    'variableName': 'SIMULATOR_API_ONLY',
                    'variableType': 'internal',
                    'value': true
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.Alias, Is.EqualTo("api-only"));
            Assert.That(p.ParameterName, Is.EqualTo("Start Simulator API"));
            Assert.That(p.ParameterType, Is.EqualTo("bool"));
            Assert.That(p.VariableType, Is.EqualTo("internal"));
            Assert.That(p.VariableName, Is.EqualTo("SIMULATOR_API_ONLY"));

            Assert.That(p.GetValue<bool>(), Is.EqualTo(true));
        }

        [Test]
        public void ParseBoolFalseParameter()
        {
            string json = @"
                {
                    'alias': 'api-only',
                    'parameterName': 'Start Simulator API',
                    'parameterType': 'bool',
                    'variableName': 'SIMULATOR_API_ONLY',
                    'variableType': 'internal',
                    'value': false
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.Alias, Is.EqualTo("api-only"));
            Assert.That(p.ParameterName, Is.EqualTo("Start Simulator API"));
            Assert.That(p.ParameterType, Is.EqualTo("bool"));
            Assert.That(p.VariableType, Is.EqualTo("internal"));
            Assert.That(p.VariableName, Is.EqualTo("SIMULATOR_API_ONLY"));

            Assert.That(p.GetValue<bool>(), Is.EqualTo(false));
        }


        [Test]
        public void ParseFloatParameter()
        {
            string json = @"
                {
                    'alias': 'api-only',
                    'parameterName': 'Rain',
                    'parameterType': 'float',
                    'variableName': 'SIMULATOR_RAIN',
                    'variableType': 'internal',
                    'value': 0.42
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.GetValue<double>, Is.EqualTo(0.42));
        }

        [Test]
        public void ParseFloatZeroParameter()
        {
            string json = @"
                {
                    'alias': 'api-only',
                    'parameterName': 'Rain',
                    'parameterType': 'float',
                    'variableName': 'SIMULATOR_RAIN',
                    'variableType': 'internal',
                    'value': 0
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.GetValue<double>, Is.EqualTo(0));
        }

        [Test]
        public void ParseStringParameter()
        {
            string json = @"
                {
                    'alias': 'some-string',
                    'parameterName': 'Rain',
                    'parameterType': 'float',
                    'variableName': 'THE_STRING',
                    'variableType': 'internal',
                    'value': 'qwerty'
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.GetValue<string>(), Is.EqualTo("qwerty"));
        }

        [Test]
        public void ParseDateTimeParameter()
        {
            string json = @"
                {
                    'alias': 'some-string',
                    'parameterName': 'Time of day',
                    'parameterType': 'datetime',
                    'variableName': 'THE_STRING',
                    'variableType': 'internal',
                    'value': '2020-06-08T12:42:58.000Z'
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.ParameterType, Is.EqualTo("datetime"));

            Assert.That(p.GetValue<DateTime>(), Is.EqualTo(new DateTime(2020, 06, 08, 12, 42, 58)));
        }

        [Test]
        public void ParseMalformerDateTimeParameter()
        {
            string json = @"
                {
                    'alias': 'some-string',
                    'parameterName': 'Rain',
                    'parameterType': 'float',
                    'variableName': 'THE_STRING',
                    'variableType': 'internal',
                    'value': 'qqqqq'
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That( 
                () => { p.GetValue<DateTime>(); },
                Throws.InstanceOf<System.FormatException>()
            );
        }

        [Test]
        public void ParseTimeOnlyDateTime()
        {
            string json = @"
                {
                    'alias': 'some-string',
                    'parameterName': 'Rain',
                    'parameterType': 'float',
                    'variableName': 'THE_STRING',
                    'variableType': 'internal',
                    'value': '17:42'
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            var dt = p.GetValue<DateTime>();

            Debug.Log($"{dt}");

            DateTime expectedDateTime = DateTime.Today.AddHours(17).AddMinutes(42);

            Assert.That(p.GetValue<DateTime>(),Is.EqualTo(expectedDateTime));
        }

        [Test]
        public void ParseDateOnlyDateTime()
        {
            string json = @"
                {
                    'alias': 'some-string',
                    'parameterName': 'Rain',
                    'parameterType': 'float',
                    'variableName': 'THE_STRING',
                    'variableType': 'internal',
                    'value': '2020-05-06'
                }";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            var dt = p.GetValue<DateTime>();

            Debug.Log($"{dt}");
            Assert.That(p.GetValue<DateTime>(), Is.EqualTo(new DateTime(2020, 05, 06)));
        }

        [Test]
        public void ParseMapParameter()
        {
            string json = @"
                {
                    'alias': 'test-map',
                    'parameterName': 'Some map',
                    'parameterType': 'map',
                    'variableName': 'SIMULATOR_MAP',
                    'variableType': 'internal',
                    'value': {
                        'id': '841b956b-7848-4c04-a017-5bd4d8385bbd',
                        'name': 'BorregasAve',
                        'assetGuid': '4c01a627-1ea9-4bf3-a52b-66debcabceaf',
                        'ownerId': '9f042740-290f-4232-b06b-2d466a51a98a',
                        'createdAt': '2020-05-28T23:32:57.000Z',
                        'updatedAt': '2020-05-28T23:35:00.000Z'
                    }
                }
            ";

            var p = JObject.Parse(json).ToObject<TemplateParameter>();

            Assert.That(p.Alias, Is.EqualTo("test-map"));
            Assert.That(p.ParameterName, Is.EqualTo("Some map"));
            Assert.That(p.ParameterType, Is.EqualTo("map"));
            Assert.That(p.VariableType, Is.EqualTo("internal"));
            Assert.That(p.VariableName, Is.EqualTo("SIMULATOR_MAP"));

            var mapValue = p.GetValue<MapData>();
            Assert.That(mapValue.Id, Is.EqualTo("841b956b-7848-4c04-a017-5bd4d8385bbd"));
            Assert.That(mapValue.Name, Is.EqualTo("BorregasAve"));
            Assert.That(mapValue.AssetGuid, Is.EqualTo("4c01a627-1ea9-4bf3-a52b-66debcabceaf"));
            Assert.That(mapValue.OwnerId, Is.EqualTo("9f042740-290f-4232-b06b-2d466a51a98a"));
        }
    }
}