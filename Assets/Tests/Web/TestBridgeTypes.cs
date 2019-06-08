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
using Simulator.Bridge;
using Simulator.Bridge.Cyber;
using Simulator.Bridge.Ros;
using Simulator.Web;
using System.Collections.Generic;
using System.Linq;

namespace Simulator.Tests.Web
{
    public class BridgeType
    {
        public string Name;
        public string[] SupportedDataTypes;
    }

    public class TestBridgeTypes
    {
        Browser Browser;

        public TestBridgeTypes()
        {
            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.RootPathProvider<UnityRootPathProvider>();
                    config.Module<BridgeTypesModule>();
                }),
                ctx => {
                    ctx.Accept("application/json");
                    ctx.HttpRequest();
                }
            );
        }

        static BridgeType GetBridgeType<Factory>() where Factory : IBridgeFactory, new()
        {
            var factory = new Factory();
            return new BridgeType()
            {
                Name = factory.Name,
                SupportedDataTypes = factory.SupportedDataTypes.Select(t => t.ToString()).ToArray(),
            };
        }

        [Test]
        public void TestRequest()
        {
            var bridgeTypes = new []
            {
                GetBridgeType<RosBridgeFactory>(),
                GetBridgeType<Ros2BridgeFactory>(),
                GetBridgeType<RosApolloBridgeFactory>(),
                GetBridgeType<CyberBridgeFactory>(),
            };
            var result = Browser.Get("/bridge-types").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var js = new JavaScriptSerializer();
            var bridges = js.Deserialize<List<BridgeType>>(result.Body.AsString());
            Assert.AreEqual(bridgeTypes.Length, bridges.Count);

            foreach(BridgeType bridgeType in bridgeTypes)
            {
               var bridge = bridges.Find(b => b.Name == bridgeType.Name);
               Assert.NotNull(bridge);
               Assert.AreEqual(bridge.SupportedDataTypes.Length, bridgeType.SupportedDataTypes.Length);

               foreach(string type in bridgeType.SupportedDataTypes)
               {
                   Assert.True(bridge.SupportedDataTypes.Contains(type));
               }
            }
        }
    }
}