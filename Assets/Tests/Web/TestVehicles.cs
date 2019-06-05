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
        Mock<IDownloadService> MockDownload;
        Mock<INotificationService> MockNotification;

        Browser Browser;

        public TestVehicles()
        {
            Mock = new Mock<IVehicleService>(MockBehavior.Strict);
            MockDownload = new Mock<IDownloadService>(MockBehavior.Strict);
            MockNotification = new Mock<INotificationService>(MockBehavior.Strict);

            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.Dependency(Mock.Object);
                    config.Dependency(MockDownload.Object);
                    config.Dependency(MockNotification.Object);
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
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get("/vehicles/foo/bar").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestList()
        {
            int page = 0; // default page
            int count = 5; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = page * count + i })
            );

            MockDownload.Reset();
            MockNotification.Reset();

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

            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOnlyPage()
        {
            int page = 123;
            int count = 5; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = page * count + i })
            );

            MockDownload.Reset();
            MockNotification.Reset();

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

            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListPageAndBadCount()
        {
            int page = 123;
            int count = 5; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = page * count + i })
            );

            MockDownload.Reset();
            MockNotification.Reset();

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

            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListPageAndCount()
        {
            int page = 123;
            int count = 30;

            Mock.Reset();
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = page * count + i })
            );

            MockDownload.Reset();
            MockNotification.Reset();

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

            Mock.Verify(srv => srv.List(page, count), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBadId()
        {
            long id = 99999999;

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGet()
        {
            long id = 123;

            var expected = new VehicleModel()
            {
                Id = id,
                Name = "vehicleName",
                Status = "Valid",
                LocalPath = "LocalPath",
                PreviewUrl = "PreviewUrl",
                Url = "Url",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var vehicle = result.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(expected.Id, vehicle.Id);
            Assert.AreEqual(expected.Name, vehicle.Name);
            Assert.AreEqual(expected.Status, vehicle.Status);
            Assert.AreEqual(expected.Url, vehicle.Url);

            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
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
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyUrl()
        {
            Mock.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            var request = new VehicleRequest()
            {
                name = "name",
                url = string.Empty,
            };

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddBadUrl()
        {
            Mock.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            var request = new VehicleRequest()
            {
                name = "name",
                url = "not^an~url",
            };

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
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
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>())).Throws<Exception>(); // TODO: we need to use more specialized exception here!

                MockDownload.Reset();
                MockNotification.Reset();

                LogAssert.Expect(LogType.Exception, new Regex("^Exception"));
                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Add(It.Is<VehicleModel>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestAddSensors()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                    sensors = "velodyne,GPS",
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                        Assert.AreEqual(request.sensors, req.Sensors);
                    })
                    .Returns(1);

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Valid", vehicle.Status);
                Assert.AreEqual(request.sensors.Length, vehicle.Sensors.Length);
                for (int i = 0; i < request.sensors.Length; i++)
                {
                    Assert.AreEqual(request.sensors[i], vehicle.Sensors[i]);
                }

                Mock.Verify(srv => srv.Add(It.Is<VehicleModel>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
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
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(id);

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Valid", vehicle.Status);
                // TODO: test vehicle.PreviewUrl

                Mock.Verify(srv => srv.Add(It.Is<VehicleModel>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestAddRemoteUrl()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 123;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "https://github.com/lgsvl/simulator/releases/download/2019.04/lgsvlsimulator-win64-2019.04.zip",
                };

                var updated = new VehicleModel()
                {
                    Name = request.name,
                    Url = request.url,
                    Status = "Whatever",
                };

                var uri = new Uri(request.url);
                var path = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));

                Mock.Reset();
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(id);
                Mock.Setup(srv => srv.Get(id)).Returns(updated);
                Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(v => v.Name == updated.Name))).Returns(1);

                MockDownload.Reset();
                MockDownload.Setup(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()))
                    .Callback<Uri, string, Action<int>, Action<bool>>((u, localpath, update, complete) =>
                    {
                        Assert.AreEqual(uri, u);
                        Assert.AreEqual(path, localpath);
                        update(100);
                        complete(true);
                    });

                MockNotification.Reset();
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>()));
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>()));

                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Downloading", vehicle.Status);

                Mock.Verify(srv => srv.Add(It.Is<VehicleModel>(m => m.Name == request.name)), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(v => v.Name == updated.Name)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.Verify(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()), Times.Once);
                MockDownload.VerifyNoOtherCalls();

                MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(2));
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestAddRemoteUrlDownloadFail()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 123;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "https://github.com/lgsvl/simulator/releases/download/2019.04/lgsvlsimulator-win64-2019.04.zip",
                };

                var updated = new VehicleModel()
                {
                    Name = request.name,
                    Url = request.url,
                    Status = "Invalid",
                };

                var uri = new Uri(request.url);
                var path = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));

                Mock.Reset();
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(id);
                Mock.Setup(srv => srv.Get(id)).Returns(updated);
                Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(v => v.Name == updated.Name))).Returns(1);

                MockDownload.Reset();
                MockDownload.Setup(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()))
                    .Callback<Uri, string, Action<int>, Action<bool>>((u, localpath, update, complete) =>
                    {
                        Assert.AreEqual(uri, u);
                        Assert.AreEqual(path, localpath);
                        update(100);
                        complete(false);
                    });

                MockNotification.Reset();
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>()));
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>()));

                var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Downloading", vehicle.Status);

                Mock.Verify(srv => srv.Add(It.Is<VehicleModel>(m => m.Name == request.name)), Times.Once);
                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(v => v.Name == updated.Name)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.Verify(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()), Times.Once);
                MockDownload.VerifyNoOtherCalls();

                MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(2));
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
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
                Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
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
                var existing = new VehicleModel()
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
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>())).Returns(2);

                MockDownload.Reset();
                MockNotification.Reset();

                LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one vehicle has id"));
                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
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
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyUrl()
        {
            Mock.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            long id = 12345;
            var request = new VehicleRequest()
            {
                name = "name",
                url = string.Empty,
            };

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadUrl()
        {
            Mock.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            long id = 12345;
            var request = new VehicleRequest()
            {
                name = "name",
                url = "not^an~url",
            };

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateDuplicateUrl()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new VehicleModel()
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
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>())).Throws<Exception>(); // TODO: we need to use more specialized exception here!

                MockDownload.Reset();
                MockNotification.Reset();

                LogAssert.Expect(LogType.Exception, new Regex("^Exception"));
                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Name == request.name)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
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
                var existing = new VehicleModel()
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
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(1);

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual(existing.Status, vehicle.Status);
                // TODO: test vehicle.PreviewUrl

                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
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
                var existing = new VehicleModel()
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
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(1);

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Valid", vehicle.Status);
                // TODO: test vehicle.PreviewUrl

                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateDifferentSensors()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new VehicleModel()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://" + temp,
                    Status = "Whatever",
                };

                var request = new VehicleRequest()
                {
                    name = existing.Name,
                    url = "file://" + temp,
                    sensors = "velodyne,GPS",
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {
                        Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                        Assert.AreEqual(request.sensors, req.Sensors);
                    })
                    .Returns(1);

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual(existing.Status, vehicle.Status);
                Assert.AreEqual(request.sensors.Length, vehicle.Sensors.Length);
                for (int i = 0; i < request.sensors.Length; i++)
                {
                    Assert.AreEqual(request.sensors[i], vehicle.Sensors[i]);
                }



                Mock.Verify(srv => srv.Get(id), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateDifferentUrlRemote()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new VehicleModel()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://" + temp,
                    Status = "Whatever",
                };

                var request = new VehicleRequest()
                {
                    name = existing.Name,
                    url = "https://github.com/lgsvl/simulator/releases/download/2019.04/lgsvlsimulator-win64-2019.04.zip",
                };

                var uri = new Uri(request.url);
                var path = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(v => v.Id == id))).Returns(1);

                MockDownload.Reset();
                MockDownload.Setup(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()))
                    .Callback<Uri, string, Action<int>, Action<bool>>((u, localpath, update, complete) =>
                    {
                        Assert.AreEqual(uri, u);
                        Assert.AreEqual(path, localpath);
                        Assert.AreEqual("Downloading", existing.Status);
                        update(100);
                        Assert.AreEqual("Downloading", existing.Status);
                        complete(true);
                    });

                MockNotification.Reset();
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>()));
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>()));

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Valid", vehicle.Status);

                Mock.Verify(srv => srv.Get(id), Times.Exactly(2));
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Exactly(2));
                Mock.VerifyNoOtherCalls();

                MockDownload.Verify(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()), Times.Once);
                MockDownload.VerifyNoOtherCalls();

                MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(2));
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateDifferentUrlRemoteDownloadFail()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "UnityFS");

                long id = 12345;
                var existing = new VehicleModel()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://" + temp,
                    Status = "Whatever",
                };

                var request = new VehicleRequest()
                {
                    name = existing.Name,
                    url = "https://github.com/lgsvl/simulator/releases/download/2019.04/lgsvlsimulator-win64-2019.04.zip",
                };

                var uri = new Uri(request.url);
                var path = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id)).Returns(existing);
                Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(v => v.Id == id))).Returns(1);

                MockDownload.Reset();
                MockDownload.Setup(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()))
                    .Callback<Uri, string, Action<int>, Action<bool>>((u, localpath, update, complete) =>
                    {
                        Assert.AreEqual(uri, u);
                        Assert.AreEqual(path, localpath);
                        Assert.AreEqual("Downloading", existing.Status);
                        update(100);
                        Assert.AreEqual("Downloading", existing.Status);
                        complete(false);
                    });

                MockNotification.Reset();
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>()));
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>()));

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);
                Assert.AreEqual("Invalid", vehicle.Status);

                Mock.Verify(srv => srv.Get(id), Times.Exactly(2));
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Exactly(2));
                Mock.VerifyNoOtherCalls();

                MockDownload.Verify(srv => srv.AddDownload(uri, path, It.IsAny<Action<int>>(), It.IsAny<Action<bool>>()), Times.Once);
                MockDownload.VerifyNoOtherCalls();

                MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(2));
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestDelete()
        {
            long id = 12345;
            var localPath = "some path";

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id)).Returns(new VehicleModel() { LocalPath = localPath });
            Mock.Setup(srv => srv.GetCountOfLocal(localPath)).Returns(1); // TODO: test if this returns more than 1
            Mock.Setup(srv => srv.Delete(id)).Returns(1);

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.Verify(srv => srv.GetCountOfLocal(localPath), Times.Once);
            Mock.Verify(srv => srv.Delete(id), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMissingId()
        {
            long id = 12345;

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id)).Throws<IndexOutOfRangeException>();

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMultipleId()
        {
            long id = 12345;
            var localPath = "some path";

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id)).Returns(new VehicleModel() { LocalPath = localPath });
            Mock.Setup(srv => srv.GetCountOfLocal(localPath)).Returns(1);
            Mock.Setup(srv => srv.Delete(It.IsAny<long>())).Returns(2);

            MockDownload.Reset();
            MockNotification.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one vehicle has id"));
            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.Verify(srv => srv.GetCountOfLocal(localPath), Times.Once);
            Mock.Verify(srv => srv.Delete(id), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }
    }
}
