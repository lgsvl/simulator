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
    public class TestVehicles
    {
        Mock<IVehicleService> Mock;
        Browser Browser;

        public TestVehicles()
        {
            Mock = new Mock<IVehicleService>(MockBehavior.Strict);

            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.Dependency(Mock.Object);
                    config.Module<VehiclesModule>();
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

            var result = Browser.Get("/vehicles/foo/bar").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);

            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestList()
        {
            int page = 0; // default page
            int count = 5; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Vehicle() { Id = page * count + i })
            );

            var result = Browser.Get($"/vehicles").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var vehicle = js.Deserialize<VehicleResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page * count + i, vehicle.Id);
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
            int count = 5; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Vehicle() { Id = page * count + i })
            );

            var result = Browser.Get($"/vehicles", ctx => ctx.Query("page", page.ToString())).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var vehicle = js.Deserialize<VehicleResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page * count + i, vehicle.Id);
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
            int count = 5; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Vehicle() { Id = page * count + i })
            );

            var result = Browser.Get($"/vehicles", ctx =>
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
                var vehicle = js.Deserialize<VehicleResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page * count + i, vehicle.Id);
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
                Enumerable.Range(0, count).Select(i => new Vehicle() { Id = page * count + i })
            );

            var result = Browser.Get($"/vehicles", ctx =>
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
                var vehicle = js.Deserialize<VehicleResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page * count + i, vehicle.Id);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBadId()
        {
            long id = 99999999;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

            var result = Browser.Get($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGet()
        {
            long id = 123;

            var expected = new Vehicle()
            {
                Id = id,
                Name = "vehicleName",
                Status = "Valid",
                LocalPath = "LocalPath",
                PreviewUrl = "PreviewUrl",
                Url = "Url",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var vehicle = result.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(expected.Id, vehicle.Id);
            Assert.AreEqual(expected.Name, vehicle.Name);
            Assert.AreEqual(expected.Status, vehicle.Status);
            Assert.AreEqual(expected.Url, vehicle.Url);

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyName()
        {
            var request = new VehicleRequest()
            {
                name = string.Empty,
                url = "file://" + Path.Combine(Config.Root, "README.md"),
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyUrl()
        {
            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var request = new VehicleRequest()
            {
                name = "name",
                url = string.Empty,
            };

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddBadUrl()
        {
            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var request = new VehicleRequest()
            {
                name = "name",
                url = "not^an~url",
            };

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddDuplicateUrl()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Add(It.IsAny<Vehicle>())).Throws<Exception>(); // TODO: we need to use more specialized exception here!

                LogAssert.Expect(LogType.Exception, new Regex("^Exception"));
                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Add(It.Is<Vehicle>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestAdd()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Add(It.IsAny<Vehicle>()))
                    .Callback<Vehicle>(req =>
                    {
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(id);

                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Valid", vehicle.Status);
                // TODO: test vehicle.PreviewUrl

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Add(It.Is<Vehicle>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestAddRemoteUrl()
        {
            Assert.Fail("not implemented");
        }

        [Test]
        public void TestUpdateMissingId()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateMultipleIds()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new Vehicle()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://" + temp,
                    Status = "Whatever",
                };
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<Vehicle>())).Returns(2);

                LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one vehicle has id"));
                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<Vehicle>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateEmptyName()
        {
            long id = 12345;
            var request = new VehicleRequest()
            {
                name = string.Empty,
                url = "file://" + Path.Combine(Config.Root, "README.md"),
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyUrl()
        {
            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            long id = 12345;
            var request = new VehicleRequest()
            {
                name = "name",
                url = string.Empty,
            };

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadUrl()
        {
            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            long id = 12345;
            var request = new VehicleRequest()
            {
                name = "name",
                url = "not^an~url",
            };

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateDuplicateUrl()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new Vehicle()
                {
                    Id = id,
                    Name = "name",
                    Url = "file://" + temp,
                };

                var request = new VehicleRequest()
                {
                    name = "different name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<Vehicle>())).Throws<Exception>(); // TODO: we need to use more specialized exception here!

                LogAssert.Expect(LogType.Exception, new Regex("^Exception"));
                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<Vehicle>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateDifferentName()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new Vehicle()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://" + temp,
                    LocalPath = "/local/path",
                    Status = "Whatever",
                };

                var request = new VehicleRequest()
                {
                    name = "name",
                    url = existing.Url,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<Vehicle>()))
                    .Callback<Vehicle>(req =>
                    {
                        Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(1);

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual(existing.Status, vehicle.Status);
                // TODO: test vehicle.PreviewUrl

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<Vehicle>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateDifferentUrl()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new Vehicle()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://old/url",
                    Status = "Whatever",
                };

                var request = new VehicleRequest()
                {
                    name = existing.Name,
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Open());
                Mock.Setup(srv => srv.Close());
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<Vehicle>()))
                    .Callback<Vehicle>(req =>
                    {
                        Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(1);

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Valid", vehicle.Status);
                // TODO: test vehicle.PreviewUrl

                Mock.Verify(srv => srv.Open(), Times.Once);
                Mock.Verify(srv => srv.Close(), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<Vehicle>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateDifferentUrlRemote()
        {
            Assert.Fail("not implemented");
        }

        [Test]
        public void TestDelete()
        {
            long id = 12345;
            var localPath = "some path";

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(new Vehicle() { LocalPath = localPath });
            Mock.Setup(srv => srv.GetCountOfLocal(localPath)).Returns(1); // TODO: test if this returns more than 1
            Mock.Setup(srv => srv.Delete(id)).Returns(1);

            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.Verify(srv => srv.GetCountOfLocal(localPath), Times.Once);
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
            Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMultipleId()
        {
            long id = 12345;
            var localPath = "some path";

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(new Vehicle() { LocalPath = localPath });
            Mock.Setup(srv => srv.GetCountOfLocal(localPath)).Returns(1);
            Mock.Setup(srv => srv.Delete(It.IsAny<long>())).Returns(2);

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one vehicle has id"));
            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.Verify(srv => srv.GetCountOfLocal(localPath), Times.Once);
            Mock.Verify(srv => srv.Delete(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }
    }
}
