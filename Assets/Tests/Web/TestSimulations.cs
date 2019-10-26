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
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulator.Tests.Web
{
    public class TestSimulations
    {
        Mock<ISimulationService> Mock;
        Mock<IUserService> MockUser;
        Mock<IClusterService> MockCluster;
        Mock<IMapService> MockMap;
        Mock<IVehicleService> MockVehicle;

        Browser Browser;

        public TestSimulations()
        {
            Mock = new Mock<ISimulationService>(MockBehavior.Strict);
            MockUser = new Mock<IUserService>(MockBehavior.Strict);
            MockCluster = new Mock<IClusterService>(MockBehavior.Strict);
            MockMap = new Mock<IMapService>(MockBehavior.Strict);
            MockVehicle = new Mock<IVehicleService>(MockBehavior.Strict);

            Browser = new Browser(
                new LoggedInBootstrapper(config =>
                {
                    config.Dependency(Mock.Object);
                    config.Dependency(MockUser.Object);
                    config.Dependency(MockCluster.Object);
                    config.Dependency(MockMap.Object);
                    config.Dependency(MockVehicle.Object);
                    config.Module<SimulationsModule>();
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

            var result = Browser.Get("/simulations/foo/bar").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);

            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestList()
        {
            int offset = 0;
            int count = Config.DefaultPageSize;

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new SimulationModel() { Id = offset * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = sim.Id.ToString(); });

            MockUser.Reset();

            var result = Browser.Get($"/simulations").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(offset * count + i, simulation.Id);
                Assert.AreEqual((offset * count + i).ToString(), simulation.Status);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()), Times.Exactly(count));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOnlyOffset()
        {
            int offset = 123;
            int count = Config.DefaultPageSize;

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new SimulationModel() { Id = offset * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = sim.Id.ToString(); });

            MockUser.Reset();

            var result = Browser.Get($"/simulations", ctx =>
            {
                ctx.Query("offset", offset.ToString());
            }).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(offset * count + i, simulation.Id);
                Assert.AreEqual((offset * count + i).ToString(), simulation.Status);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()), Times.Exactly(count));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOffsetAndBadCount()
        {
            int offset = 123;
            int count = Config.DefaultPageSize;

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new SimulationModel() { Id = offset * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = sim.Id.ToString(); });

            MockUser.Reset();

            var result = Browser.Get($"/simulations", ctx =>
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
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(offset * count + i, simulation.Id);
                Assert.AreEqual((offset * count + i).ToString(), simulation.Status);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()), Times.Exactly(count));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestListOffsetAndCount()
        {
            int offset = 123;
            int count = 30;

            Mock.Reset();
            Mock.Setup(srv => srv.List(null, offset, count, "Test User")).Returns(
                Enumerable.Range(0, count).Select(i => new SimulationModel() { Id = offset * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = sim.Id.ToString(); });

            MockUser.Reset();

            var result = Browser.Get($"/simulations", ctx =>
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
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(offset * count + i, simulation.Id);
                Assert.AreEqual((offset * count + i).ToString(), simulation.Status);
            }

            Mock.Verify(srv => srv.List(null, offset, count, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()), Times.Exactly(count));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBadId()
        {
            long id = 999999999;

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Throws<IndexOutOfRangeException>();

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGet()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                Vehicles = new []{ new ConnectionModel{
                    Simulation = 1,
                    Vehicle = 1,
                    Connection = "",
                    },
                },
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Vehicles.Length, simulation.Vehicles.Length);
            for (int i = 0; i < expected.Vehicles.Length; i++)
            {
                Assert.AreEqual(expected.Vehicles[i].Vehicle, simulation.Vehicles[i].Vehicle);
                Assert.AreEqual(expected.Vehicles[i].Connection, simulation.Vehicles[i].Connection);
            }

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetMultipleVehicles()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                Vehicles = new[]{ new ConnectionModel{
                    Simulation = 1,
                    Vehicle = 1,
                    Connection = "www.example.com/index.html",
                    },new ConnectionModel{
                    Simulation = 1,
                    Vehicle = 2,
                    Connection = "",
                    },new ConnectionModel{
                    Simulation = 1,
                    Vehicle = 3,
                    Connection = "www.example.com/index.html",
                    },
                },
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Vehicles.Length, simulation.Vehicles.Length);
            for (int i = 0; i < expected.Vehicles.Length; i++)
            {
                Assert.AreEqual(expected.Vehicles[i].Vehicle, simulation.Vehicles[i].Vehicle);
                Assert.AreEqual(expected.Vehicles[i].Connection, simulation.Vehicles[i].Connection);
            }


            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == expected.Name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetCluster()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                Cluster = 456,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Cluster, simulation.Cluster);


            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == expected.Name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetMap()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                Map = 456,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Map, simulation.Map);

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == expected.Name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBools()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                ApiOnly = false,
                Interactive = true,
                Headless = true,
                UseTraffic = false,
                UseBicyclists = true,
                UsePedestrians = false,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.ApiOnly, simulation.ApiOnly);
            Assert.AreEqual(expected.Interactive, simulation.Interactive);
            Assert.AreEqual(expected.Headless, simulation.Headless);
            Assert.AreEqual(expected.UseTraffic, simulation.UseTraffic);
            Assert.AreEqual(expected.UseBicyclists, simulation.UseBicyclists);
            Assert.AreEqual(expected.UsePedestrians, simulation.UsePedestrians);


            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == expected.Name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetTime()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                TimeOfDay = System.DateTime.Now,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.TimeOfDay, simulation.TimeOfDay);


            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == expected.Name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetWeather()
        {
            long id = 123;

            var expected = new SimulationModel()
            {
                Id = id,
                Name = "name",
                Rain = 0.5f,
                Fog = 0.5f,
                Wetness = 0.5f,
                Cloudiness = 0.2f,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(expected);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Rain, simulation.Weather.rain);
            Assert.AreEqual(expected.Fog, simulation.Weather.fog);
            Assert.AreEqual(expected.Wetness, simulation.Weather.wetness);
            Assert.AreEqual(expected.Cloudiness, simulation.Weather.cloudiness);


            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == expected.Name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyName()
        {
            var request = new SimulationRequest()
            {
                name = string.Empty,
                apiOnly = true,
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyMap()
        {
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = null,
                cluster = 0,
                vehicles = new[] { new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                },
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyCluster()
        {
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = 1,
                cluster = null,
                vehicles = new[] { new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "www.example.com/index.html",
                    },
                },
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddEmptyVehicles()
        {
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = 1,
                cluster = 0,
                vehicles = Array.Empty<ConnectionRequest>(),
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddDuplicateVehicles()
        {
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = 1,
                cluster = 0,
                vehicles = new[] {
                    new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "www.example.com/index.html",
                    },
                    new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "www.example.com/index.html",
                    },
                },
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestConvertSeed()
        {
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = 1,
                cluster = 0,
                vehicles = new[] { new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                },
                seed = 5,
            };

            SimulationModel model = request.ToModel("Test User");
            Assert.AreEqual(request.seed, model.Seed);
            SimulationResponse response = SimulationResponse.Create(model);
            Assert.AreEqual(model.Seed, response.Seed);
        }

        [Test]
        public void TestAddWithSeed()
        {
            long id = 111;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = 1,
                cluster = 0,
                vehicles = new[]
                {
                    new ConnectionRequest()
                    {
                        Vehicle = 1,
                        Connection = "www.example.com/index.html",
                    },
                },
                seed = 5,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.IsAny<SimulationModel>()))
                .Callback<SimulationModel>(req =>
                {
                    Assert.AreEqual(request.name, req.Name);
                    Assert.AreEqual(request.apiOnly, req.ApiOnly);
                    Assert.AreEqual(request.map, req.Map);
                    Assert.AreEqual(request.cluster, req.Cluster);
                    Assert.AreEqual(request.vehicles.Length, req.Vehicles.Length);
                    Assert.AreEqual(request.seed, req.Seed);
                })
                .Returns(id);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim2, bool allow) => { sim2.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var sim = result.Body.DeserializeJson<SimulationResponse>();

            Assert.AreEqual("Valid", sim.Status);

            Mock.Verify(srv => srv.Add(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()), Times.Exactly(2));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddBadRain()
        {
            var Weather = new Weather()
            {
                rain = 10f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddBadFog()
        {
            var Weather = new Weather()
            {
                fog = 8026150f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddBadWetness()
        {
            var Weather = new Weather()
            {
                wetness = 15f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddBadCloudiness()
        {
            var Weather = new Weather()
            {
                cloudiness = 21f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddInvalid()
        {
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == true)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Invalid"; });

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Simulation is invalid"));
            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddApiOnly()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
                map = 1,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.IsAny<SimulationModel>()))
                .Callback<SimulationModel>(req =>
                {
                    Assert.AreEqual(request.name, req.Name);
                    Assert.AreEqual(request.apiOnly, req.ApiOnly);
                })
                .Returns(id);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(id, simulation.Id);
            Assert.AreEqual(request.cluster, simulation.Cluster);
            Assert.Null(simulation.Seed);
            Assert.AreEqual("Valid", simulation.Status);

            Mock.Verify(srv => srv.Add(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()), Times.Exactly(2));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddNoApi()
        {
            long id = 234;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                map = 1,
                cluster = 0,
                vehicles = new[]
                {
                    new ConnectionRequest()
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                },
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Add(It.IsAny<SimulationModel>()))
                .Callback<SimulationModel>(req =>
                {
                    Assert.AreEqual(request.name, req.Name);
                    Assert.AreEqual(request.apiOnly, req.ApiOnly);
                    Assert.AreEqual(request.map, req.Map);
                    Assert.AreEqual(request.cluster, req.Cluster);
                    Assert.AreEqual(request.vehicles.Length, req.Vehicles.Length);
                })
                .Returns(id);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(request.map, simulation.Map);
            Assert.AreEqual(request.cluster, simulation.Cluster);
            Assert.Null(simulation.Seed);
            Assert.AreEqual("Valid", simulation.Status);

            for (int i = 0; i < request.vehicles.Length; i++)
            {
                Assert.AreEqual(request.vehicles[i].Vehicle, simulation.Vehicles[i].Vehicle);
                Assert.AreEqual(request.vehicles[i].Connection, simulation.Vehicles[i].Connection);
            }

            Mock.Verify(srv => srv.Add(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()), Times.Exactly(2));
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMissingId()
        {
            long id = 1234;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new SimulationModel()
            {
                Owner = null,
            });

            Mock.Setup(srv => srv.Update(It.IsAny<SimulationModel>())).Returns(0);

            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);

            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == "name"), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMultipleIds()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new SimulationModel()
            {
                Owner = null,
            });

            Mock.Setup(srv => srv.Update(It.IsAny<SimulationModel>())).Returns(2);

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one simulation has id"));
            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);

            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == "name"), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyName()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = string.Empty,
                apiOnly = true,
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));



            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyCluster()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = null,
                map = 1,
                vehicles = new[] { new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                },
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyMap()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = 1,
                map = null,
                vehicles = new[] { new ConnectionRequest
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                },
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyVehicles()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = 1,
                map = 1,
                vehicles = Array.Empty<ConnectionRequest>(),
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateDuplicateVehicles()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = 1,
                map = 1,
                vehicles = new[]
                {
                    new ConnectionRequest()
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                    new ConnectionRequest()
                    {
                        Vehicle = 1,
                        Connection = "",
                    },
                },
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadRain()
        {
            long id = 123;
            var Weather = new Weather()
            {
                rain = 10f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadFog()
        {
            long id = 123;
            var Weather = new Weather()
            {
                fog = 10f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();
            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadWetness()
        {
            long id = 123;
            var Weather = new Weather()
            {
                wetness = 10f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();
            MockUser.Reset();
            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadCloudiness()
        {
            long id = 123;
            var Weather = new Weather()
            {
                cloudiness = 10f,
            };
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                weather = Weather,
            };

            Mock.Reset();
            MockUser.Reset();
            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateInvalid()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == true)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Invalid"; });

            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new SimulationModel()
            {
                Owner = null,
            });

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Simulation is invalid"));
            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdate()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
            };

            Mock.Reset();

            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new SimulationModel()
            {
                Owner = null,
            });

            Mock.Setup(srv => srv.Update(It.IsAny<SimulationModel>())).Returns(1);

            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(id, simulation.Id);
            Assert.AreEqual(request.cluster, simulation.Cluster);
            Assert.AreEqual("Valid", simulation.Status);

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<SimulationModel>(s => s.Name == request.name && s.Owner == null)), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()), Times.Exactly(2));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateSeed()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
                seed = 1,
            };

            Mock.Reset();

            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new SimulationModel()
            {
                Owner = null,
            });

            Mock.Setup(srv => srv.Update(It.IsAny<SimulationModel>())).Returns(1);

            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(id, simulation.Id);
            Assert.AreEqual(request.cluster, simulation.Cluster);
            Assert.AreEqual("Valid", simulation.Status);
            Assert.AreEqual(request.seed, simulation.Seed);

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);

            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()), Times.Exactly(2));

            Mock.VerifyNoOtherCalls();
            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateNullSeed()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
                cluster = 5,
            };

            Mock.Reset();

            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(new SimulationModel()
            {
                Owner = null,
            });

            Mock.Setup(srv => srv.Update(It.IsAny<SimulationModel>())).Returns(1);

            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.IsAny<bool>()))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = "Valid"; });

            MockUser.Reset();

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(id, simulation.Id);
            Assert.AreEqual(request.cluster, simulation.Cluster);
            Assert.AreEqual("Valid", simulation.Status);
            Assert.Null(simulation.Seed);

            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);

            Mock.Verify(srv => srv.Update(It.Is<SimulationModel>(s => s.Name == request.name)), Times.Once);

            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == request.name), It.IsAny<bool>()), Times.Exactly(2));

            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMissingId()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Delete(id, "Test User")).Returns(0);

            MockUser.Reset();

            var result = Browser.Delete($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Delete(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDeleteMultipleIds()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Delete(id, "Test User")).Returns(2);

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: More than one simulation has id"));
            var result = Browser.Delete($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Delete(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestDelete()
        {
            long id = 56;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Delete(id, "Test User")).Returns(1);

            MockUser.Reset();

            var result = Browser.Delete($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Delete(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStartAlreadyRunning()
        {
            long id = 45;
            var existing = new SimulationModel()
            {
                Id = 45,
                Name = "name",
                ApiOnly = true,
                Status = "Whatever"
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns(existing);

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Simulation with id"));
            var result = Browser.Post($"/simulations/{id}/start").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStartMissingId()
        {
            long id = 45;

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns((SimulationModel)null);
            Mock.Setup(srv => srv.Get(id, "Test User")).Throws<IndexOutOfRangeException>();

            MockUser.Reset();

            var result = Browser.Post($"/simulations/{id}/start").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStartNotValid()
        {
            long id = 45;
            var existing = new SimulationModel()
            {
                Id = id,
                Name = "name",
                ApiOnly = true,
                Status = "Not Valid",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns((SimulationModel)null);
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = existing.Status; });
            Mock.Setup(srv => srv.Update(It.IsAny<SimulationModel>())).Returns(1);

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Cannot start an invalid simulation"));

            var result = Browser.Post($"/simulations/{id}/start").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == existing.Name), It.IsAny<bool>()), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<SimulationModel>(s => s.Name == existing.Name)), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStart()
        {
            long id = 45;
            var existing = new SimulationModel()
            {
                Id = id,
                Name = "name",
                ApiOnly = true,
                Status = "Valid",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns((SimulationModel)null);
            Mock.Setup(srv => srv.Get(id, "Test User")).Returns(existing);
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<SimulationModel>(), It.Is<bool>(x => x == false)))
                .Callback((SimulationModel sim, bool allow) => { sim.Status = existing.Status; });
            Mock.Setup(srv => srv.Start(It.IsAny<SimulationModel>()));

            MockUser.Reset();

            var result = Browser.Post($"/simulations/{id}/start").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.Verify(srv => srv.Get(id, "Test User"), Times.Once);
            Mock.Verify(srv => srv.GetActualStatus(It.Is<SimulationModel>(s => s.Name == existing.Name), It.IsAny<bool>()), Times.Once);
            Mock.Verify(srv => srv.Start(It.Is<SimulationModel>(s => s.Name == existing.Name)), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStopNotRunning()
        {
            long id = 45;

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns((SimulationModel)null);

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Simulation with id"));
            var result = Browser.Post($"/simulations/{id}/stop").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStopWrongId()
        {
            long id = 45;
            var existing = new SimulationModel()
            {
                Id = id - 1,
                Name = "name",
                ApiOnly = true,
                Status = "Valid",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns(existing);

            MockUser.Reset();

            LogAssert.Expect(LogType.Exception, new Regex("^Exception: Simulation with id"));
            var result = Browser.Post($"/simulations/{id}/stop").Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }

        [Test]
        public void TestStop()
        {
            long id = 45;
            var existing = new SimulationModel()
            {
                Id = id,
                Name = "name",
                ApiOnly = true,
                Status = "Valid",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.GetCurrent("Test User")).Returns(existing);
            Mock.Setup(srv => srv.Stop());

            MockUser.Reset();

            var result = Browser.Post($"/simulations/{id}/stop").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.GetCurrent("Test User"), Times.Once);
            Mock.Verify(srv => srv.Stop(), Times.Once);
            Mock.VerifyNoOtherCalls();

            MockUser.VerifyNoOtherCalls();
        }
    }
}
