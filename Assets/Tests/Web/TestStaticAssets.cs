/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.IO;
using Nancy;
using Nancy.Testing;
using NUnit.Framework;
using Simulator.Web;
using Simulator.Web.Modules;

namespace Simulator.Tests.Web
{
    public class TestStaticAssets
    {
        Browser Browser;

        public TestStaticAssets()
        {
            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.RootPathProvider<UnityRootPathProvider>();
                    config.Module<IndexModule>();
                }),
                ctx => ctx.HttpRequest()
            );
        }

        void TestRequest(string path, string contentType, string asset)
        {
            var result = Browser.Get(path).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual(contentType, result.ContentType);

            var expected = File.ReadAllText(Path.Combine(Config.Root, "WebUI", "dist", asset));
            var body = result.Body.AsString();
            Assert.AreEqual(expected, body);
        }

        [Test]
        public void TestIndex()
        {
            TestRequest("/", "text/html", "index.html");
        }

        [Test]
        public void TestMainJs()
        {
            TestRequest("/main.js", "application/javascript", "main.js");
        }

        [Test]
        public void TestMainCss()
        {
            TestRequest("/main.css", "text/css", "main.css");
        }
    }
}
