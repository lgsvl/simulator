/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using UnityEngine;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using FluentValidation;
using Simulator.Database;
using Simulator.Database.Services;
using Nancy.Extensions;

namespace Simulator.Web.Modules
{
    public class Weather
    {
        public float? rain;
        public float? fog;
        public float? wetness;
        public float? cloudiness;
    }

    public class ConnectionRequest
    {
        public long Vehicle;
        public string Connection;

        public static ConnectionModel ToModel(ConnectionRequest connection)
        {
            return new ConnectionModel()
            {
                Vehicle = connection.Vehicle,
                Connection = connection.Connection
            };
        }
    }

    public class ConnectionResponse
    {
        public long Vehicle;
        public string Connection;

        public static ConnectionResponse Create(ConnectionModel connection)
        {
            return new ConnectionResponse()
            {
                Vehicle = connection.Vehicle,
                Connection = connection.Connection
            };
        }
    }

    public class SimulationRequest
    {
        public string name;
        public long? map;
        public ConnectionRequest[] vehicles;
        public bool? apiOnly;
        public bool? interactive;
        public bool? headless;
        public int? seed;
        public bool? useTraffic;
        public bool? useBicyclists;
        public bool? usePedestrians;
        public long? cluster;
        public DateTime? timeOfDay;
        public Weather weather;

        public SimulationModel ToModel(string owner)
        {
            return new SimulationModel()
            {
                Owner = owner,
                Name = name,
                Map = map,
                Vehicles = vehicles?.Select(connectionRequest => ConnectionRequest.ToModel(connectionRequest)).ToArray(),
                ApiOnly = apiOnly,
                Interactive = interactive,
                Headless = headless,
                Cluster = cluster,
                TimeOfDay = timeOfDay,
                Rain = weather?.rain,
                Fog = weather?.fog,
                Cloudiness = weather?.cloudiness,
                Wetness = weather?.wetness,
                Seed = seed,
                UseTraffic = useTraffic,
                UseBicyclists = useBicyclists,
                UsePedestrians = usePedestrians,
            };
        }
    }

    public class SimulationResponse
    {
        public long Id;
        public string Name;
        public string Status;
        public long? Map;
        public ConnectionResponse[] Vehicles;
        public bool? ApiOnly;
        public bool? Interactive;
        public bool? Headless;
        public bool? UseTraffic;
        public bool? UsePedestrians;
        public bool? UseBicyclists;
        public long? Cluster;
        public DateTime? TimeOfDay;
        public Weather Weather;
        public int? Seed;
        public string Error;

        public static SimulationResponse Create(SimulationModel simulation)
        {
            return new SimulationResponse()
            {
                Id = simulation.Id,
                Name = simulation.Name,
                Status = simulation.Status,
                Map = simulation.Map,
                Vehicles = simulation.Vehicles?.Select(connectionModel => ConnectionResponse.Create(connectionModel)).ToArray(),
                ApiOnly = simulation.ApiOnly,
                Interactive = simulation.Interactive,
                Headless = simulation.Headless,
                Cluster = simulation.Cluster,
                TimeOfDay = simulation.TimeOfDay,
                Weather = new Weather()
                {
                    rain = simulation.Rain,
                    fog = simulation.Fog,
                    wetness = simulation.Wetness,
                    cloudiness = simulation.Cloudiness,
                },
                Seed = simulation.Seed,
                UseTraffic = simulation.UseTraffic,
                UseBicyclists = simulation.UseBicyclists,
                UsePedestrians = simulation.UsePedestrians,
                Error = simulation.Error,
            };
        }
    }

    public class SimulationRequestValidation : AbstractValidator<SimulationRequest>
    {
        public SimulationRequestValidation()
        {
            RuleFor(req => req.name)
                .NotEmpty().WithMessage("You must enter a non-empty name");

            RuleFor(req => req.apiOnly)
                .NotNull().WithMessage("Api only parameter must be specified");

            RuleFor(req => req.cluster)
                .NotNull().WithMessage("You must specifiy a cluster");

            When(req => req.apiOnly.HasValue && !req.apiOnly.Value, () =>
            {
                RuleFor(req => req.map).NotNull().WithMessage("You must specifiy a map");
                RuleFor(req => req.vehicles).NotNull().WithMessage("You must specify at least one vehicle")
                    .Must(vehicles => vehicles.Length > 0).WithMessage("You must specify at least one vehicle")
                    .Must(vehicles => vehicles.Length == vehicles.DistinctBy(v => new { v.Vehicle, v.Connection }).Count()).WithMessage("Vehicles must not be exact duplicates");
            });

            When(req => req.weather != null, () =>
            {
                RuleFor(req => req.weather.rain).Cascade(CascadeMode.StopOnFirstFailure)
                    .InclusiveBetween(0, 1).WithMessage("Rain value {PropertyValue} must be between 0 and 1")
                    .When(req => req.weather.rain.HasValue);

                RuleFor(req => req.weather.fog).Cascade(CascadeMode.StopOnFirstFailure)
                    .InclusiveBetween(0, 1).WithMessage("Fog value {PropertyValue} must be between 0 and 1")
                    .When(req => req.weather.fog.HasValue);

                RuleFor(req => req.weather.wetness).Cascade(CascadeMode.StopOnFirstFailure)
                    .InclusiveBetween(0, 1).WithMessage("Wetness value {PropertyValue} must be between 0 and 1")
                    .When(req => req.weather.wetness.HasValue);

                RuleFor(req => req.weather.cloudiness).Cascade(CascadeMode.StopOnFirstFailure)
                    .InclusiveBetween(0, 1).WithMessage("Cloudiness value {PropertyValue} must be between 0 and 1")
                    .When(req => req.weather.cloudiness.HasValue);
            });

            // TODO
            RuleFor(req => req.weather).Must(BeValidWeather);
        }

        public bool BeValidWeather(Weather w)
        {
            if (w != null)
            {
                if (w.cloudiness != null)
                {
                    if (w.cloudiness < 0 || w.cloudiness > 1) return false;
                }
                if (w.fog != null)
                {
                    if (w.fog < 0 || w.fog > 1) return false;
                }
                if (w.rain != null)
                {
                    if (w.rain < 0 || w.rain > 1) return false;
                }
                if (w.cloudiness != null)
                {
                    if (w.wetness < 0 || w.wetness > 1) return false;
                }
            }

            return true;
        }
    }

    public class SimulationsModule : NancyModule
    {
        InlineValidator<SimulationModel> startValidator = new InlineValidator<SimulationModel>();

        public SimulationsModule(ISimulationService service, IUserService userService, IClusterService clusterService, IMapService mapService, IVehicleService vehicleService) : base("simulations")
        {
            this.RequiresAuthentication();

            Get("/", x =>
            {
                Debug.Log($"Listing simulations");
                try
                {
                    string filter = Request.Query["filter"];
                    int offset = Request.Query["offset"];
                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : Config.DefaultPageSize;
                    return service.List(filter, offset, count, this.Context.CurrentUser.Identity.Name).Select(sim =>
                    {
                        service.GetActualStatus(sim, false);
                        return SimulationResponse.Create(sim);
                    }).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to list simulations: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Get("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Getting simulation with id {id}");
                try
                {
                    var simulation = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    if (simulation.TimeOfDay.HasValue)
                    {
                        simulation.TimeOfDay = DateTime.SpecifyKind(simulation.TimeOfDay.Value, DateTimeKind.Utc);
                    }
                    service.GetActualStatus(simulation, false);
                    return SimulationResponse.Create(simulation);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Simulation with id {id} does not exist");
                    return Response.AsJson(new { error = $"Simulation with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to get simulation with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/", x =>
            {
                Debug.Log($"Adding new simulation");
                try
                {
                    var req = this.BindAndValidate<SimulationRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Wrong request: {message}");
                        return Response.AsJson(new { error = $"Failed to add simulation: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var simulation = req.ToModel(this.Context.CurrentUser.Identity.Name);

                    service.GetActualStatus(simulation, true);
                    if (simulation.Status != "Valid")
                    {
                        throw new Exception($"Simulation is invalid");
                    }

                    service.GetActualStatus(simulation, false);
                    long id = service.Add(simulation);
                    Debug.Log($"Simulation added with id {id}");
                    simulation.Id = id;

                    SIM.LogWeb(SIM.Web.SimulationAddName, simulation.Name);
                    try
                    {
                        SIM.LogWeb(SIM.Web.SimulationAddMapName, mapService.Get(simulation.Map.Value, this.Context.CurrentUser.Identity.Name).Name);

                        if (simulation.Vehicles != null)
                        {
                            foreach (var vehicle in simulation.Vehicles)
                            {
                                var vehicleModel = vehicleService.Get(vehicle.Vehicle, this.Context.CurrentUser.Identity.Name);
                                SIM.LogWeb(SIM.Web.SimulationAddVehicleName, vehicleModel.Name);
                                SIM.LogWeb(SIM.Web.SimulationAddBridgeType, vehicleModel.BridgeType);
                            }
                        }

                        SIM.LogWeb(SIM.Web.SimulationAddAPIOnly, simulation.ApiOnly.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddInteractiveMode, simulation.Interactive.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddHeadlessMode, simulation.Headless.ToString());
                        try
                        {
                            SIM.LogWeb(SIM.Web.SimulationAddClusterName, clusterService.Get(simulation.Cluster.Value, this.Context.CurrentUser.Identity.Name).Name);
                        }
                        catch { };
                        SIM.LogWeb(SIM.Web.SimulationAddUsePredefinedSeed, simulation.Seed.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddEnableNPC, simulation.UseTraffic.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddRandomPedestrians, simulation.UsePedestrians.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddTimeOfDay, simulation.TimeOfDay.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddRain, simulation.Rain.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddWetness, simulation.Wetness.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddFog, simulation.Fog.ToString());
                        SIM.LogWeb(SIM.Web.SimulationAddCloudiness, simulation.Cloudiness.ToString());
                    }
                    catch { };

                    return SimulationResponse.Create(simulation);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to add simulation: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Updating simulation with id {id}");
                try
                {
                    var req = this.BindAndValidate<SimulationRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Wrong request: {message}");
                        return Response.AsJson(new { error = $"Failed to update simulation: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var original = service.Get(id, Context.CurrentUser.Identity.Name);

                    var simulation = req.ToModel(original.Owner);
                    simulation.Id = id;

                    service.GetActualStatus(simulation, true);
                    if (simulation.Status != "Valid")
                    {
                        throw new Exception($"Simulation is invalid");
                    }

                    service.GetActualStatus(simulation, false);
                    int result = service.Update(simulation);

                    SIM.LogWeb(SIM.Web.SimulationEditName, simulation.Name);
                    try
                    {
                        SIM.LogWeb(SIM.Web.SimulationEditMapName, mapService.Get(simulation.Map.Value, this.Context.CurrentUser.Identity.Name).Name);

                        if (simulation.Vehicles != null)
                        {
                            foreach (var vehicle in simulation.Vehicles)
                            {
                                try
                                {
                                    var vehicleModel = vehicleService.Get(vehicle.Vehicle, this.Context.CurrentUser.Identity.Name);
                                    SIM.LogWeb(SIM.Web.SimulationEditVehicleName, vehicleModel.Name);
                                    SIM.LogWeb(SIM.Web.SimulationEditBridgeType, vehicleModel.BridgeType);
                                }
                                catch
                                {
                                }
                            }
                        }

                        SIM.LogWeb(SIM.Web.SimulationEditAPIOnly, simulation.ApiOnly.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditInteractiveMode, simulation.Interactive.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditHeadlessMode, simulation.Headless.ToString());
                        try
                        {
                            SIM.LogWeb(SIM.Web.SimulationEditClusterName, clusterService.Get(simulation.Cluster.Value, this.Context.CurrentUser.Identity.Name).Name);
                        }
                        catch { };
                        SIM.LogWeb(SIM.Web.SimulationEditUsePredefinedSeed, simulation.Seed.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditEnableNPC, simulation.UseTraffic.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditRandomPedestrians, simulation.UsePedestrians.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditTimeOfDay, simulation.TimeOfDay.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditRain, simulation.Rain.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditWetness, simulation.Wetness.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditFog, simulation.Fog.ToString());
                        SIM.LogWeb(SIM.Web.SimulationEditCloudiness, simulation.Cloudiness.ToString());
                    }
                    catch
                    {
                    }

                    if (result > 1)
                    {
                        throw new Exception($"More than one simulation has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return SimulationResponse.Create(simulation);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Simulation with id {id} does not exist");
                    return Response.AsJson(new { error = $"Simulation with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to update simulation with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Delete("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Removing simulation with id {id}");
                try
                {
                    int result = service.Delete(id, this.Context.CurrentUser.Identity.Name);
                    SIM.LogWeb(SIM.Web.SimulationDelete);
                    if (result > 1)
                    {
                        throw new Exception($"More than one simulation has id {id}");
                    }

                    if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Simulation with id {id} does not exist");
                    return Response.AsJson(new { error = $"Simulation with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to remove simulation with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/{id:long}/start", x =>
            {
                long id = x.id;
                Debug.Log($"Starting simulation with id {id}");
                try
                {
                    var current = service.GetCurrent(this.Context.CurrentUser.Identity.Name);
                    if (current != null)
                    {
                        throw new Exception($"Simulation with id {current.Id} is already running");
                    }

                    var simulation = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    service.GetActualStatus(simulation, false);
                    if (simulation.Status != "Valid")
                    {
                        simulation.Status = "Invalid";
                        service.Update(simulation);

                        throw new Exception("Cannot start an invalid simulation");
                    }

                    service.Start(simulation);
                    SIM.LogWeb(SIM.Web.WebClick, "SimulationStart");
                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Simulation with id {id} does not exist");
                    return Response.AsJson(new { error = $"Simulation with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to start simulation with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/{id:long}/stop", x =>
            {
                long id = x.id;
                Debug.Log($"Stopping simulation with id {id}");
                try
                {
                    var simulation = service.GetCurrent(this.Context.CurrentUser.Identity.Name);
                    if (simulation == null || simulation.Id != id)
                    {
                        throw new Exception($"Simulation with id {id} is not running");
                    }

                    service.Stop();
                    SIM.LogWeb(SIM.Web.WebClick, "SimulationStop");
                    return new { };
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to stop simulation with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });
        }
    }
}
