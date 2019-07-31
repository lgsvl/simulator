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

        public SimulationModel ToModel()
        {
            return new SimulationModel()
            {
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
        public bool? UseTraffic { get; set; }
        public bool? UsePedestrians { get; set; }
        public bool? UseBicyclists { get; set; }
        public long? Cluster;
        public DateTime? TimeOfDay;
        public Weather Weather;
        public int? Seed;

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
                .NotNull().WithMessage("You specify if the API will be used");

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

        public SimulationsModule(ISimulationService service) : base("simulations")
        {
            Get("/", x =>
            {
                Debug.Log($"Listing simulations");
                try
                {
                    int page = Request.Query["page"];

                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : Config.DefaultPageSize;
                    return service.List(page, count).Select(sim =>
                    {
                        sim.Status = service.GetActualStatus(sim, false);
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
                    var simulation = service.Get(id);
                    if (simulation.TimeOfDay.HasValue)
                    {
                        simulation.TimeOfDay = DateTime.SpecifyKind(simulation.TimeOfDay.Value, DateTimeKind.Utc);
                    }
                    simulation.Status = service.GetActualStatus(simulation, false);
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

                    var simulation = req.ToModel();
                    simulation.Status = service.GetActualStatus(simulation, true);
                    if (simulation.Status != "Valid")
                    {
                        throw new Exception($"Simulation is invalid");
                    }

                    simulation.Status = service.GetActualStatus(simulation, false);
                    long id = service.Add(simulation);
                    Debug.Log($"Simulation added with id {id}");
                    simulation.Id = id;

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

                    var simulation = req.ToModel();
                    simulation.Id = id;
                    simulation.Status = service.GetActualStatus(simulation, true);
                    if (simulation.Status != "Valid")
                    {
                        throw new Exception($"Simulation is invalid");
                    }

                    simulation.Status = service.GetActualStatus(simulation, false);
                    int result = service.Update(simulation);
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
                    int result = service.Delete(id);
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
                    var current = service.GetCurrent();
                    if (current != null)
                    {
                        throw new Exception($"Simulation with id {current.Id} is already running");
                    }

                    var simulation = service.Get(id);
                    if (service.GetActualStatus(simulation, false) != "Valid")
                    {
                        simulation.Status = "Invalid";
                        service.Update(simulation);

                        throw new Exception("Cannot start an invalid simulation");
                    }

                    service.Start(simulation);
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
                    var simulation = service.GetCurrent();
                    if (simulation == null || simulation.Id != id)
                    {
                        throw new Exception($"Simulation with id {id} is not running");
                    }

                    service.Stop();
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
