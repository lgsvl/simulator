/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using Nancy;
using Nancy.Json;
using Nancy.Json.Simple;
using Nancy.Testing;
using NUnit.Framework;
using Moq;
using Simulator.Database;
using Simulator.Database.Services;
using Simulator.Web.Modules;
using Simulator.Web;

namespace Simulator.Tests.Web
{
    public class TestClusters
    {
        Mock<IClusterService> Mock;
        Browser Browser;

        public TestClusters()
        {
            Mock = new Mock<IClusterService>(MockBehavior.Strict);

            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.Dependency(Mock.Object);
                    config.Module<ClustersModule>();
                }),
                ctx =>
                {
                    ctx.Accept("application/json");
                    ctx.HttpRequest();
                }
            );
        }

        [Test]
        public void TestBadRoute()
        {
            Mock.Reset();

            var result = Browser.Get("/clusters/foo/bar").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);

            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestList()
        {
            int page = 0;
            int count = 5;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Cluster() { Id = page * count + i })
            );

            var result = Browser.Get($"/clusters").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list  = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var cluster = js.Deserialize<ClusterResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, cluster.Id);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOnlyPage()
        {
            int page = 123;
            int count = 5;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Cluster() { Id = page * count + i })
            );

            var result = Browser.Get($"/clusters", ctx => ctx.Query("page", page.ToString())).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var cluster = js.Deserialize<ClusterResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, cluster.Id);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListPageAndBadCount()
        {
            int page = 123;
            int count = 5;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Cluster() { Id = page * count + i })
            );

            var result = Browser.Get($"/clusters", ctx => 
            {
                ctx.Query("page", page.ToString());
                ctx.Query("count", "0");
            }).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var cluster = js.Deserialize<ClusterResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, cluster.Id);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListPageAndCount()
        {
            int page = 123;
            int count = 30;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Cluster() { Id = page * count + i })
            );

            var result = Browser.Get($"/clusters", ctx => 
            {
                ctx.Query("page", page.ToString());
                ctx.Query("count", count.ToString());
            }).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var cluster = js.Deserialize<ClusterResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, cluster.Id);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBadId()
        {
            long id = 999999999;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

            var result = Browser.Get($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetDefault()
        {
            long id = 0;

            var expected = new Cluster()
            {
                Id = id,
                Name = "Local Machine",
                Ips = "127.0.0.1",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var cluster = result.Body.DeserializeJson<ClusterResponse>();
            Assert.AreEqual(expected.Id, cluster.Id);
            Assert.AreEqual(expected.Name, cluster.Name);
            var ipArray = expected.Ips.Split(',');
            Assert.AreEqual(ipArray.Length, cluster.Ips.Length);
            for (int i=0; i<ipArray.Length; i++)
            {
                Assert.AreEqual(ipArray[i], cluster.Ips[i]);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGet()
        {
            long id = 123;

            var expected = new Cluster()
            {
                Id = id,
                Name = "ClusterName",
                Ips = "LocalHost",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var cluster = result.Body.DeserializeJson<ClusterResponse>();
            Assert.AreEqual(expected.Id, cluster.Id);
            Assert.AreEqual(expected.Name, cluster.Name);
            var ipArray = expected.Ips.Split(',');
            Assert.AreEqual(ipArray.Length, cluster.Ips.Length);
            for (int i=0; i<ipArray.Length; i++)
            {
                Assert.AreEqual(ipArray[i], cluster.Ips[i]);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }
        
        [Test]
        public void TestGetMultipleIps()
        {
            long id  = 12345;
            var expected = new Cluster()
            {
                Id = id,
                Name = "ClusterName",
                Ips = "LocalHost,127.0.0.1",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var cluster = result.Body.DeserializeJson<ClusterResponse>();
            Assert.AreEqual(expected.Id, cluster.Id);
            Assert.AreEqual(expected.Name, cluster.Name);
            var ipArray = expected.Ips.Split(',');
            Assert.AreEqual(ipArray.Length, cluster.Ips.Length);
            for (int i=0; i<ipArray.Length; i++)
            {
                Assert.AreEqual(ipArray[i], cluster.Ips[i]);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyName()
        {
            // long id = 12345;
            var request = new ClusterRequest()
            {
                name = string.Empty,
                ips = new [] {"127.0.0.1"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/clusters", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyIps()
        {
            // long id = 12345;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = Array.Empty<string>(),
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/clusters", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddDuplcateIps()
        {
            var request = new ClusterRequest()
            {
                name = "name",
                ips = new [] {"localhost", "localhost"},
            };
            // long id = 12345;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/clusters", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAdd()
        {
            long id = 12345;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = new [] {"localhost"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Add(It.IsAny<Cluster>()))
            .Callback<Cluster>(req =>
            {
                Assert.AreEqual(request.name, req.Name);
                Assert.AreEqual(request.ips.Length, req.Ips.Split(',').Length);
            })
            .Returns(id);

            var result = Browser.Post($"/clusters", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var cluster = result.Body.DeserializeJson<ClusterResponse>();
            Assert.AreEqual(id, cluster.Id);
            Assert.AreEqual(request.name, cluster.Name);
            Assert.AreEqual(request.ips.Length, cluster.Ips.Length);
            for (int i=0; i<request.ips.Length; i++)
            {
                Assert.AreEqual(request.ips[i], cluster.Ips[i]);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Add(It.Is<Cluster>(c => c.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMissingId()
        {
            long id = 12345;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = new [] {"localhost"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Update(It.IsAny<Cluster>())).Returns(0);

            var result = Browser.Put($"/clusters/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<Cluster>(c => c.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMultipleIds()
        {
            long id = 12345;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = new [] {"localhost"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Update(It.IsAny<Cluster>())).Returns(2);

            var result = Browser.Put($"/clusters/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<Cluster>(c => c.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyName()
        {
            long id = 12345;
            var request = new ClusterRequest()
            {
                name = string.Empty,
                ips = new [] {"localhost"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/clusters/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyIps()
        {
            long id = 12345;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = Array.Empty<string>(),
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/clusters/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdate()
        {
            long id = 12345;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = new [] {"localhost"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Update(It.IsAny<Cluster>())).Returns(1);

            var result = Browser.Put($"/clusters/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var cluster = result.Body.DeserializeJson<ClusterResponse>();
            Assert.AreEqual(request.name, cluster.Name);
            Assert.AreEqual(id, cluster.Id);
            for (int i=0; i<request.ips.Length; i++)
            {
                Assert.AreEqual(request.ips[i], cluster.Ips[i]);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<Cluster>(c => c.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateDefault()
        {
            long id = 0;
            var request = new ClusterRequest()
            {
                name = "name",
                ips = new [] {"localhost"},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Cannot edit default cluster"));
            var result = Browser.Put($"/clusters/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
        }
        
        [Test]
        public void TestDelete()
        {
            long id = 12345;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Delete(id)).Returns(1);

            var result = Browser.Delete($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Delete(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMissingId()
        {
            long id = 12345;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Delete(It.IsAny<long>())).Returns(0);

            var result = Browser.Delete($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Delete(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMultipleId()
        {
            long id = 123;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Delete(It.IsAny<long>())).Returns(2);

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one cluster has id"));
            var result = Browser.Delete($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Delete(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteDefault()
        {
            long id = 0;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Cannot remove default cluster"));
            var result = Browser.Delete($"/clusters/{id}").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }
    }
}