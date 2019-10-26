/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Moq;
using Nancy;
using Nancy.Json;
using Nancy.Json.Simple;
using Nancy.Testing;
using NUnit.Framework;
using Simulator.Database;
using Simulator.Database.Services;
using Simulator.Web;
using Simulator.Web.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulator.Tests.Web
{
    public class TestVehicles
    {
        Mock<IVehicleService> Mock;
        Mock<IUserService> MockUser;
        Mock<IDownloadService> MockDownload;
        Mock<INotificationService> MockNotification;

        Browser Browser;

        public TestVehicles()
        {
            Mock = new Mock<IVehicleService>(MockBehavior.Strict);
            MockUser = new Mock<IUserService>(MockBehavior.Strict);
            MockDownload = new Mock<IDownloadService>(MockBehavior.Strict);
            MockNotification = new Mock<INotificationService>(MockBehavior.Strict);

            Browser = new Browser(
                new LoggedInBootstrapper(config =>
                {
                    config.Dependency(Mock.Object);
                    config.Dependency(MockUser.Object);
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
            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get("/vehicles/foo/bar").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestList()
        {
            int offset = 0; // default offset
            int count = Config.DefaultPageSize; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = offset * count + i, LocalPath = "file:///does/not/exist" })
            );

            MockUser.Reset();

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
                Assert.AreEqual(offset * count + i, vehicle.Id);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOnlyOffset()
        {
            int offset = 123;
            int count = Config.DefaultPageSize; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = offset * count + i, LocalPath = "file:///does/not/exist" })
            );

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get($"/vehicles", ctx => ctx.Query("offset", offset.ToString())).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var vehicle = js.Deserialize<VehicleResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(offset * count + i, vehicle.Id);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOffsetAndBadCount()
        {
            int offset = 123;
            int count = Config.DefaultPageSize; // default count

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = offset * count + i, LocalPath = "file:///does/not/exist" })
            );

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get($"/vehicles", ctx =>
            {
                ctx.Query("offset", offset.ToString());
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
                Assert.AreEqual(offset * count + i, vehicle.Id);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOffsetAndCount()
        {
            int offset = 123;
            int count = 30;

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new VehicleModel() { Id = offset * count + i, LocalPath = "file:///does/not/exist" })
            );

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get($"/vehicles", ctx =>
            {
                ctx.Query("offset", offset.ToString());
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
                Assert.AreEqual(offset * count + i, vehicle.Id);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBadId()
        {
            long id = 99999999;

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Throws<IndexOutOfRangeException>();
            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Get($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();

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
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            MockUser.Reset();
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
            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();

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
            MockUser.Reset();
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
            MockUser.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddSensors()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                    sensors = "[]",
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {   Assert.AreEqual(request.name, req.Name);
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
        public void TestAddDifferentURLs()
        {
            long id1 = 0;
            var request1 = new VehicleRequest()
            {
                name = "Car1",
                url = "http://example.com/asset-bundle",
            };

            long id2 = 1;
            var request2 = new VehicleRequest()
            {
                name = "Car2",
                url = "http://other.com/asset-bundle",
            };

            var matchingUrl1 = request1.ToModel("Test User");
            matchingUrl1.Status = "Downloading";

            var matchingUrl2 = request2.ToModel("Test User");
            matchingUrl2.Status = "Downloading";

            List<string> paths = new List<string>();

            Mock.Reset();
            Mock.SetupSequence(srv => srv.Add(It.IsAny<VehicleModel>()))
                .Returns(id1)
                .Returns(id2);
            Mock.SetupSequence(srv => srv.GetCountOfUrl(It.IsAny<string>()))
                .Returns(0)
                .Returns(1)
                .Returns(0)
                .Returns(1);
            Mock.SetupSequence(srv => srv.GetAllMatchingUrl(It.IsAny<string>()))
                .Returns(new List<VehicleModel>() { matchingUrl1 })
                .Returns(new List<VehicleModel>() { matchingUrl1, matchingUrl2 });

            MockUser.Reset();
            MockDownload.Reset();
            MockDownload.Setup(srv => srv.AddDownload(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()))
                .Callback<Uri, string, Action<int>, Action<bool, Exception>>((u, localpath, update, complete) => paths.Add(localpath));

            MockNotification.Reset();

            var result1 = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request1)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result1.StatusCode);
            Assert.That(result1.ContentType.StartsWith("application/json"));

            var vehicle1 = result1.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(id1, vehicle1.Id);
            Assert.AreEqual(request1.name, vehicle1.Name);
            Assert.AreEqual(request1.url, vehicle1.Url);
            Assert.AreEqual("Downloading", vehicle1.Status);

            var result2 = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request2)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result2.StatusCode);
            Assert.That(result2.ContentType.StartsWith("application/json"));

            var vehicle2 = result2.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(id2, vehicle2.Id);
            Assert.AreEqual(request2.name, vehicle2.Name);
            Assert.AreEqual(request2.url, vehicle2.Url);
            Assert.AreEqual("Downloading", vehicle2.Status);

            Assert.AreEqual(2, paths.Count);
            Assert.AreNotEqual(paths[0], paths[1]);

            Mock.Verify(srv => srv.Add(It.IsAny<VehicleModel>()), Times.Exactly(2));
            Mock.Verify(srv => srv.GetCountOfUrl(It.IsAny<string>()), Times.Exactly(4));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();

            MockDownload.Verify(srv => srv.AddDownload(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()), Times.Exactly(2));
            MockDownload.VerifyNoOtherCalls();

            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddSameURL()
        {
            long id1 = 1;
            var request1 = new VehicleRequest()
            {
                name = "Car1",
                url = "http://example.com/asset-bundle",
            };

            long id2 = 2;
            var request2 = new VehicleRequest()
            {
                name = "Car2",
                url = request1.url,
            };

            List<string> paths = new List<string>();

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.Is<VehicleModel>(v => v.Name == request1.name)))
                .Callback<VehicleModel>(req =>
                {
                    Assert.AreEqual(request1.url, req.Url);
                    paths.Add(req.LocalPath);
                })
                .Returns(id1);
            Mock.Setup(srv => srv.Add(It.Is<VehicleModel>(v => v.Name == request2.name)))
                .Callback<VehicleModel>(req =>
                {
                    Assert.AreEqual(request2.url, req.Url);
                    paths.Add(req.LocalPath);
                })
                .Returns(id2);
            Mock.SetupSequence(srv => srv.GetCountOfUrl(It.Is<string>(s => s == request1.url)))
                .Returns(0)
                .Returns(0)
                .Returns(1)
                .Returns(1);

            MockUser.Reset();

            MockDownload.Reset();
            MockDownload.Setup(srv => srv.AddDownload(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()));

            MockNotification.Reset();

            var result1 = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request1)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result1.StatusCode);
            Assert.That(result1.ContentType.StartsWith("application/json"));

            var vehicle1 = result1.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(id1, vehicle1.Id);
            Assert.AreEqual(request1.name, vehicle1.Name);
            Assert.AreEqual(request1.url, vehicle1.Url);
            Assert.AreEqual("Downloading", vehicle1.Status);

            Mock.Setup(srv => srv.GetAllMatchingUrl(It.Is<string>(s => s == request1.url)))
                .Returns(new List<VehicleModel>()
                {new VehicleModel()
                {
                    LocalPath = paths[0],
                    Status = vehicle1.Status,
                },
                });

            var result2 = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request2)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result2.StatusCode);
            Assert.That(result2.ContentType.StartsWith("application/json"));

            var vehicle2 = result2.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(id2, vehicle2.Id);
            Assert.AreEqual(request2.name, vehicle2.Name);
            Assert.AreEqual(request2.url, vehicle2.Url);
            Assert.AreEqual(vehicle1.Status, vehicle2.Status);

                Mock.Verify(srv => srv.Add(It.IsAny<VehicleModel>()), Times.Exactly(2));
                Mock.Verify(srv => srv.GetCountOfUrl(It.Is<string>(s => s == request1.url)), Times.Exactly(4));
                Mock.Verify(srv => srv.GetAllMatchingUrl(It.Is<string>(s => s == request1.url)), Times.Once);
                Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();

            MockDownload.Verify(srv => srv.AddDownload(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()), Times.Once);
            MockDownload.VerifyNoOtherCalls();

            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddSameURLLocal()
        {
            long id1 = 1;
            var request1 = new VehicleRequest()
            {
                name = "Car2",
                url = "http://example.com/asset-bundle",
            };

            List<string> paths = new List<string>();

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.Is<VehicleModel>(v => v.Name == request1.name)))
                .Callback<VehicleModel>(req =>
                {
                    Assert.AreEqual(request1.url, req.Url);
                    paths.Add(req.LocalPath);
                })
                .Returns(id1);
            Mock.SetupSequence(srv => srv.GetCountOfUrl(It.Is<string>(s => s == request1.url)))
                .Returns(1)
                .Returns(1);
            Mock.Setup(srv => srv.GetAllMatchingUrl(request1.url)).Returns(
                new List<VehicleModel> {   new VehicleModel {       Name = "Car1",
                            Url = "http://example.com/asset-bundle",
                            Status = "Valid",
                            LocalPath = "some path",
                        },
                });

            MockNotification.Reset();

            var result1 = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request1)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result1.StatusCode);
            Assert.That(result1.ContentType.StartsWith("application/json"));

            var vehicle1 = result1.Body.DeserializeJson<VehicleResponse>();
            Assert.AreEqual(id1, vehicle1.Id);
            Assert.AreEqual(request1.name, vehicle1.Name);
            Assert.AreEqual(request1.url, vehicle1.Url);
            Assert.AreEqual("Valid", vehicle1.Status);

            Mock.Verify(srv => srv.Add(It.IsAny<VehicleModel>()), Times.Once);
            Mock.Verify(srv => srv.GetCountOfUrl(It.Is<string>(s => s == request1.url)), Times.Exactly(2));
            Mock.Verify(srv => srv.GetAllMatchingUrl(It.Is<string>(s => s == request1.url)), Times.Once);
            Mock.Verify(srv => srv.GetAllMatchingUrl(request1.url), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAdd()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

                long id = 12345;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {   Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(id);

                MockUser.Reset();

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

                MockUser.VerifyNoOtherCalls();
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
            long id = 123;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "https://github.com/lgsvl/simulator/releases/download/2019.04/lgsvlsimulator-win64-2019.04.zip",
                };

                var uri = new Uri(request.url);
                var path = Path.Combine(Config.PersistentDataPath, "Vehicle_" + Guid.NewGuid().ToString());

                var updated = new VehicleModel()
                {
                    Name = request.name,
                    Owner = "Test User",
                    Id = id,
                    Url = request.url,
                    Status = "Whatever",
                    LocalPath = path,
                };

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                .Callback<VehicleModel>(req =>
                {   Assert.AreEqual(request.name, req.Name);
                    Assert.AreEqual(request.url, req.Url);
                })
                .Returns(id);
            Mock.SetupSequence(srv => srv.GetCountOfUrl(updated.Url))
                .Returns(0)
                .Returns(1);
            Mock.Setup(srv => srv.GetAllMatchingUrl(request.url)).Returns(new List<VehicleModel> { updated });
            Mock.Setup(srv => srv.SetStatusForPath("Valid", It.IsAny<string>()));

            MockUser.Reset();

            MockDownload.Reset();
            MockDownload.Setup(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()))
                .Callback<Uri, string, Action<int>, Action<bool, Exception>>((u, localpath, update, complete) =>
                {
                    Assert.AreEqual(uri, u);
                    update(100);
                    complete(true, null);
                });

            MockNotification.Reset();
            MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>(), "Test User"));
            MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>(), "Test User"));

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

                MockUser.VerifyNoOtherCalls();

            MockDownload.Verify(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()), Times.Once);
            MockDownload.VerifyNoOtherCalls();

            MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>(), "Test User"), Times.Exactly(2));
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddRemoteUrlDownloadFail()
        {
            long id = 123;
            var request = new VehicleRequest()
            {
                name = "name",
                url = "https://github.com/lgsvl/simulator/releases/download/2019.04/lgsvlsimulator-win64-2019.04.zip",
            };

            var updated = new VehicleModel()
            {
                Name = request.name,
                Owner = "Test User",
                Id = id,
                Url = request.url,
                Status = "Invalid",
            };

            var uri = new Uri(request.url);
            var path = Path.Combine(Config.PersistentDataPath, "Vehicle_" + Guid.NewGuid().ToString());

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.IsAny<VehicleModel>()))
                .Callback<VehicleModel>(req =>
                {   Assert.AreEqual(request.name, req.Name);
                    Assert.AreEqual(request.url, req.Url);
                })
                .Returns(id);
            Mock.Setup(srv => srv.GetCountOfUrl(updated.Url)).Returns(1);
            Mock.Setup(srv => srv.GetAllMatchingUrl(updated.Url)).Returns(new List<VehicleModel> { updated });
            Mock.Setup(srv => srv.SetStatusForPath("Invalid", It.IsAny<string>()));

            MockUser.Reset();

            MockDownload.Reset();
            MockDownload.Setup(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()))
                .Callback<Uri, string, Action<int>, Action<bool, Exception>>((u, localpath, update, complete) =>
                {
                    Assert.AreEqual(uri, u);
                    update(100);
                    complete(false, new Exception("Test Exception"));
                });

            MockNotification.Reset();
            MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>(), "Test User"));
            MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>(), "Test User"));

            var result = Browser.Post($"/vehicles", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

                MockUser.VerifyNoOtherCalls();

            MockDownload.Verify(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()), Times.AtLeastOnce);
            MockDownload.VerifyNoOtherCalls();

            MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMissingId()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

                long id = 12345;
                var request = new VehicleRequest()
                {
                    name = "name",
                    url = "file://" + temp,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Throws<IndexOutOfRangeException>();

                MockUser.Reset();

                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();
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
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

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
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>())).Returns(2);

                MockUser.Reset();
                MockDownload.Reset();
                MockNotification.Reset();

                LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one vehicle has id"));
                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();

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
            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
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

            Mock.Reset();
            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Put($"/vehicles/{id}", ctx =>
            {


                ctx.JsonBody(request);
            }).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
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

            Mock.Reset();
            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();


            var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateDifferentName()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

                long id = 12345;
                var existing = new VehicleModel()
                {
                    Id = id,
                    Name = "ExistingName",
                    Owner = "Test User",
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
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {   Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(1);

                MockUser.Reset();
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

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();
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
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

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
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {   Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                    })
                    .Returns(1);

                MockUser.Reset();
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

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();
                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestUpdateExistingUrl()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

                long id = 12345;
                var toBeUpdated = new VehicleModel()
                {
                    Id = id,
                    Name = "ExistingName",
                    Url = "file://old/url",
                    Status = "Whatever",
                };

                var existing = new VehicleModel()
                {
                    Id = 2,
                    Name = "Already here",
                    Url = "https://example.com/asset-bundle",
                    Status = "Valid",
                    LocalPath = "some path"
                };

                var request = new VehicleRequest()
                {
                    name = toBeUpdated.Name,
                    url = existing.Url,
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(toBeUpdated);
                Mock.Setup(srv => srv.GetCountOfUrl(existing.Url)).Returns(1);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {   Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(existing.LocalPath, req.LocalPath);
                        Assert.AreEqual(existing.Url, req.Url);
                    })
                    .Returns(1);
                Mock.Setup(srv => srv.GetAllMatchingUrl(existing.Url)).Returns(new List<VehicleModel> { existing });
                Mock.Setup(srv => srv.GetAllMatchingUrl(toBeUpdated.Url)).Returns(new List<VehicleModel> { toBeUpdated });

                MockUser.Reset();
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

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.GetCountOfUrl(existing.Url), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(v => v.Name == request.name)), Times.Once);
                Mock.Verify(srv => srv.GetAllMatchingUrl(existing.Url), Times.Once);
                Mock.Verify(srv => srv.GetAllMatchingUrl(toBeUpdated.Url), Times.Once);

                Mock.VerifyNoOtherCalls();
                MockUser.VerifyNoOtherCalls();
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
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

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
                    sensors = "[]",
                };

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
                Mock.Setup(srv => srv.Update(It.IsAny<VehicleModel>()))
                    .Callback<VehicleModel>(req =>
                    {   Assert.AreEqual(id, req.Id);
                        Assert.AreEqual(request.name, req.Name);
                        Assert.AreEqual(request.url, req.Url);
                        Assert.AreEqual(request.sensors, req.Sensors);
                    })
                    .Returns(1);

                MockUser.Reset();
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

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();
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
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

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
                var path = Path.Combine(Config.PersistentDataPath, "Vehicle_" + Guid.NewGuid().ToString());

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
                Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(v => v.Id == id))).Returns(1);
                Mock.Setup(srv => srv.GetAllMatchingUrl(existing.Url)).Returns(new List<VehicleModel> { new VehicleModel { Id = id, LocalPath = path, Url = request.url } });
                Mock.Setup(srv => srv.SetStatusForPath("Valid", It.IsAny<string>()));
                Mock.Setup(srv => srv.GetCountOfUrl(request.url)).Returns(0);

                MockUser.Reset();

                MockDownload.Reset();
                MockDownload.Setup(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()))
                    .Callback<Uri, string, Action<int>, Action<bool, Exception>>((u, localpath, update, complete) =>
                    {   Assert.AreEqual(uri, u);
                        Assert.AreEqual("Downloading", existing.Status);
                        update(100);
                        Assert.AreEqual("Downloading", existing.Status);
                        complete(true, null);
                    });

                MockNotification.Reset();
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>(), It.IsAny<string>()));
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>(), It.IsAny<string>()));

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.Verify(srv => srv.GetAllMatchingUrl("file://" + temp), Times.Once);
                Mock.Verify(srv => srv.SetStatusForPath("Valid", It.IsAny<string>()), Times.Once);
                Mock.Verify(srv => srv.GetCountOfUrl(request.url), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();

                MockDownload.Verify(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()), Times.Once);
                MockDownload.VerifyNoOtherCalls();

                MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
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
                File.WriteAllBytes(temp, new byte[] { 80, 75, 3, 4 });

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
                var path = Path.Combine(Config.PersistentDataPath, "Vehicle_" + Guid.NewGuid().ToString());

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
                Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(v => v.Id == id))).Returns(1);
                Mock.Setup(srv => srv.GetAllMatchingUrl(existing.Url)).Returns(new List<VehicleModel> { new VehicleModel { Id = id, LocalPath = path, Url = request.url } });
                Mock.Setup(srv => srv.SetStatusForPath("Invalid", It.IsAny<string>()));
                Mock.Setup(srv => srv.GetCountOfUrl(request.url)).Returns(0);

                MockUser.Reset();

                MockDownload.Reset();
                MockDownload.Setup(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()))
                    .Callback<Uri, string, Action<int>, Action<bool, Exception>>((u, localpath, update, complete) =>
                    {   Assert.AreEqual(uri, u);
                        Assert.AreEqual("Downloading", existing.Status);
                        update(100);
                        Assert.AreEqual("Downloading", existing.Status);
                        complete(false, new Exception("Test Exception"));
                    });

                MockNotification.Reset();
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownload"), It.IsAny<object>(), It.IsAny<string>()));
                MockNotification.Setup(srv => srv.Send(It.Is<string>(s => s == "VehicleDownloadComplete"), It.IsAny<object>(), It.IsAny<string>()));

                var result = Browser.Put($"/vehicles/{id}", ctx => ctx.JsonBody(request)).Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                var vehicle = result.Body.DeserializeJson<VehicleResponse>();
                Assert.AreEqual(id, vehicle.Id);
                Assert.AreEqual(request.name, vehicle.Name);
                Assert.AreEqual(request.url, vehicle.Url);

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
                Mock.Verify(srv => srv.GetAllMatchingUrl("file://" + temp), Times.Once);
                Mock.Verify(srv => srv.SetStatusForPath("Invalid", It.IsAny<string>()), Times.Once);
                Mock.Verify(srv => srv.GetCountOfUrl(request.url), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();

                MockDownload.Verify(srv => srv.AddDownload(uri, It.IsAny<string>(), It.IsAny<Action<int>>(), It.IsAny<Action<bool, Exception>>()), Times.Once);
                MockDownload.VerifyNoOtherCalls();

                MockNotification.Verify(srv => srv.Send(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
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
            var temp = Path.GetTempFileName();
            try
            {
                long id = 12345;
                var localPath = temp;
                var url = "http://some.url.com";

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new VehicleModel() { LocalPath = localPath, Url = url, Owner = "Test User" });
                Mock.Setup(srv => srv.Delete(id, "Test User")).Returns(1);
                Mock.Setup(srv => srv.GetCountOfUrl(url)).Returns(1);

                MockUser.Reset();
                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Delete($"/vehicles/{id}").Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.Delete(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.GetCountOfUrl(url), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();

                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }

        [Test]
        public void TestDeleteMissingId()
        {
            long id = 12345;

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Throws<IndexOutOfRangeException>();

            MockUser.Reset();

            MockDownload.Reset();
            MockNotification.Reset();

            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMultipleId()
        {
            long id = 12345;
            var localPath = "some path";
            var url = "http://some.url.com";

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new VehicleModel() { LocalPath = localPath, Url = url, Owner = "Test User" });
            //Mock.Setup(srv => srv.GetCountOfLocal(localPath)).Returns(1);
            Mock.Setup(srv => srv.Delete(It.IsAny<long>(), "Test User")).Returns(2);
            Mock.Setup(srv => srv.GetCountOfUrl(url)).Returns(1);

            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one vehicle has id"));

            var result = Browser.Delete($"/vehicles/{id}").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            //Mock.Verify(srv => srv.GetCountOfLocal(localPath), Times.Once);
            Mock.Verify(srv => srv.Delete(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetCountOfUrl(url), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();

            MockDownload.VerifyNoOtherCalls();
            MockNotification.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMultipleLocalPaths()
        {
            var temp = Path.GetTempFileName();
            try
            {
                long id = 1;
                var url = "some url";
                var localPath = temp;

                Mock.Reset();
                Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new VehicleModel() { Url = url, LocalPath = localPath, Owner = "Test User" });
                Mock.Setup(srv => srv.GetCountOfUrl(url)).Returns(2);
                Mock.Setup(srv => srv.Delete(id, "Test User")).Returns(1);

                MockUser.Reset();
                MockDownload.Reset();
                MockNotification.Reset();

                var result = Browser.Delete($"/vehicles/{id}").Result;

                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.That(result.ContentType.StartsWith("application/json"));

                Assert.True(File.Exists(temp));

                Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
                Mock.Verify(srv => srv.GetCountOfUrl(url), Times.Once);
                Mock.Verify(srv => srv.Delete(id, "Test User"), Times.Once);
                Mock.VerifyNoOtherCalls();

                MockUser.VerifyNoOtherCalls();
                MockDownload.VerifyNoOtherCalls();
                MockNotification.VerifyNoOtherCalls();
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TestDownloadCancel()
        {
            var id = 12345;

            var vehicle = new VehicleModel()
            {
                Id = id,
                Url = "http://example.com",
                LocalPath = "some path",
                Status = "Downloading",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(vehicle);
            Mock.Setup(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id))).Returns(1);

            MockUser.Reset();
            MockDownload.Reset();
            MockDownload.Setup(srv => srv.StopDownload(vehicle.Url));

            MockNotification.Reset();

            var result = Browser.Put($"/vehicles/{id}/cancel").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Assert.AreEqual("Invalid", vehicle.Status);

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<VehicleModel>(m => m.Id == id)), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();

            MockDownload.Verify(srv => srv.StopDownload(vehicle.Url), Times.Once);
            MockDownload.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDownloadCancelWrong()
        {
            long id = 12345;

            var vehicle = new VehicleModel()
            {
                Id = id,
                Url = "http://example.com",
                LocalPath = "some path",
                Status = "Valid",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(vehicle);

            MockUser.Reset();
            MockDownload.Reset();
            MockNotification.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Failed to cancel Vehicle download: Vehicle with id"));

            var result = Browser.Put($"/vehicles/{id}/cancel").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Assert.AreEqual("Valid", vehicle.Status);

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }
    }}
