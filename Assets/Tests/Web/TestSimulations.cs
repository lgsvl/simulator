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
    public class TestSimulations
    {
        Mock<ISimulationService> Mock;
        Browser Browser;

        public TestSimulations()
        {
            Mock = new Mock<ISimulationService>(MockBehavior.Strict);

            Browser = new Browser(
                new ConfigurableBootstrapper(config =>
                {
                    config.Dependency(Mock.Object);
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
            int page = 0;
            int count = 5;

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.List(page, count)).Returns(
                Enumerable.Range(0, count).Select(i => new Simulation() { Id = page * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<Simulation>()))
            .Returns((Simulation sim) => sim.Id.ToString());

            var result = Browser.Get($"/simulations").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list  = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, simulation.Id);
                Assert.AreEqual((page*count+i).ToString(), simulation.Status);
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
                Enumerable.Range(0, count).Select(i => new Simulation() { Id = page * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<Simulation>()))
            .Returns((Simulation sim) => sim.Id.ToString());

            var result = Browser.Get($"/simulations", ctx => ctx.Query("page", page.ToString())).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var list = SimpleJson.DeserializeObject(result.Body.AsString()) as List<object>;
            Assert.AreEqual(count, list.Count);

            var js = new JavaScriptSerializer();
            for (int i = 0; i < count; i++)
            {
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, simulation.Id);
                Assert.AreEqual((page*count+i).ToString(), simulation.Status);
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
                Enumerable.Range(0, count).Select(i => new Simulation() { Id = page * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<Simulation>()))
            .Returns((Simulation sim) => sim.Id.ToString());

            var result = Browser.Get($"/simulations", ctx => 
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
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, simulation.Id);
                Assert.AreEqual((page*count+i).ToString(), simulation.Status);
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
                Enumerable.Range(0, count).Select(i => new Simulation() { Id = page * count + i })
            );
            Mock.Setup(srv => srv.GetActualStatus(It.IsAny<Simulation>()))
            .Returns((Simulation sim) => sim.Id.ToString());

            var result = Browser.Get($"/simulations", ctx => 
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
                var simulation = js.Deserialize<SimulationResponse>(SimpleJson.SerializeObject(list[i]));
                Assert.AreEqual(page*count+i, simulation.Id);
                Assert.AreEqual((page*count+i).ToString(), simulation.Status);
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

            var result = Browser.Get($"/simulations/{id}").Result;

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

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                Vehicles = "1",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            var vehicles = expected.Vehicles.Split(',');
            Assert.AreEqual(vehicles.Length, simulation.Vehicles.Length);
            for (int i=0; i<vehicles.Length; i++)
            {
                Assert.AreEqual(vehicles[i], simulation.Vehicles[i].ToString());
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetMultipleVehicles()
        {
            long id = 123;

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                Vehicles = "1,2,3",
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            var vehicles = expected.Vehicles.Split(',');
            Assert.AreEqual(vehicles.Length, simulation.Vehicles.Length);
            for (int i=0; i<vehicles.Length; i++)
            {
                Assert.AreEqual(vehicles[i], simulation.Vehicles[i].ToString());
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetCluster()
        {
            long id  = 123;

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                Cluster = 456,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Cluster, simulation.Cluster);
            
            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetMap()
        {
            long id  = 123;

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                Map = 456,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.Map, simulation.Map);
            
            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetBools()
        {
            long id  = 123;

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                ApiOnly = false,
                Interactive = true,
                OffScreen = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.ApiOnly, simulation.ApiOnly);
            Assert.AreEqual(expected.Interactive, simulation.Interactive);
            Assert.AreEqual(expected.OffScreen, simulation.OffScreen);
            
            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetTime()
        {
            long id  = 123;

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                TimeOfDay = System.DateTime.Now,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

            var result = Browser.Get($"/simulations/{id}").Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(expected.Id, simulation.Id);
            Assert.AreEqual(expected.Name, simulation.Name);
            Assert.AreEqual(expected.TimeOfDay, simulation.TimeOfDay);
            
            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestGetWeather()
        {
            long id  = 123;

            var expected = new Simulation()
            {
                Id = id,
                Name = "name",
                Rain = 0.5f,
                Fog = 0.5f,
                Wetness = 0.5f,
                Cloudiness = 0.2f,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Get(id)).Returns(expected);

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
            
            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Get(id), Times.Once);
            Mock.VerifyNoOtherCalls();
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
                vehicles = new long[] {1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
                vehicles = new long[] {1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
                vehicles = Array.Empty<long>(),
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
                vehicles = new long[] {1,1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestAddApiOnly()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Add(It.IsAny<Simulation>()))
            .Callback<Simulation>(req =>
            {
                Assert.AreEqual(request.name, req.Name);
                Assert.AreEqual(request.apiOnly, req.ApiOnly);
            })
            .Returns(id);

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Add(It.Is<Simulation>(s => s.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
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
                vehicles = new long[] {1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Add(It.IsAny<Simulation>()))
            .Callback<Simulation>(req =>
            {
                Assert.AreEqual(request.name, req.Name);
                Assert.AreEqual(request.apiOnly, req.ApiOnly);
                Assert.AreEqual(request.map, req.Map);
                Assert.AreEqual(request.cluster, req.Cluster);
                Assert.AreEqual(request.vehicles.Length, req.Vehicles.Length);
            })
            .Returns(id);

            var result = Browser.Post($"/simulations", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(request.map, simulation.Map);
            Assert.AreEqual(request.cluster, simulation.Cluster);
            for (int i=0; i<request.vehicles.Length; i++)
            {
                Assert.AreEqual(request.vehicles[i], simulation.Vehicles[i]);
            }

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Add(It.Is<Simulation>(s => s.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMissingId()
        {
            long id = 1234;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Update(It.IsAny<Simulation>())).Returns(0);

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<Simulation>(s => s.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateMultipleIds()
        {
            long id =123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Update(It.IsAny<Simulation>())).Returns(2);

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<Simulation>(s => s.Name == request.name)), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyName()
        {
            long id =123;
            var request = new SimulationRequest()
            {
                name = string.Empty,
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyCluster()
        {
            long id =123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = null,
                map = 1,
                vehicles = new long[] {1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyMap()
        {
            long id =123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = 1,
                map = null,
                vehicles = new long[] {1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateEmptyVehicles()
        {
            long id =123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = 1,
                map = 1,
                vehicles = Array.Empty<long>(),
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateDuplicateVehicles()
        {
            long id =123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = false,
                cluster = 1,
                map = 1,
                vehicles = new long[] {1,1},
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadRain()
        {
            long id =123;
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadFog()
        {
            long id =123;
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadWetness()
        {
            long id =123;
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdateBadCloudiness()
        {
            long id =123;
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.VerifyNoOtherCalls();
        }

        [Test]
        public void TestUpdate()
        {
            long id = 123;
            var request = new SimulationRequest()
            {
                name = "name",
                apiOnly = true,
            };

            Mock.Reset();
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Update(It.IsAny<Simulation>())).Returns(1);

            var result = Browser.Put($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));

            var simulation = result.Body.DeserializeJson<SimulationResponse>();
            Assert.AreEqual(request.name, simulation.Name);
            Assert.AreEqual(request.apiOnly, simulation.ApiOnly);
            Assert.AreEqual(id, simulation.Id);
            Assert.AreEqual("Valid", simulation.Status);

            Mock.Verify(srv => srv.Open(), Times.Once);
            Mock.Verify(srv => srv.Close(), Times.Once);
            Mock.Verify(srv => srv.Update(It.Is<Simulation>(s => s.Name == request.name)));
            Mock.VerifyNoOtherCalls();
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Delete(id)).Returns(0);

            var result = Browser.Delete($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Delete(id)).Returns(2);

            var result = Browser.Delete($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
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
            Mock.Setup(srv => srv.Open());
            Mock.Setup(srv => srv.Close());
            Mock.Setup(srv => srv.Delete(id)).Returns(1);

            var result = Browser.Delete($"/simulations/{id}", ctx => ctx.JsonBody(request)).Result;

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.That(result.ContentType.StartsWith("application/json"));
        }
    }
}