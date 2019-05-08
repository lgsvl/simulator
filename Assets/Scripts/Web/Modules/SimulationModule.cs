using Database;
using FluentValidation;
using FluentValidation.Results;
using Nancy;
using PetaPoco;
using System;
using System.Linq;
using UnityEngine;

namespace Web.Modules
{
    public class SimulationRequest
    {
        public string name;
        public int? map;
        public int[] vehicles;
        public bool? apiOnly;
        public bool? interactive;
        public bool? offScreen;
        public int? cluster;
        public DateTime? timeOfDay;
        public Weather weather;
    }

    public class SimulationResponse : WebResponse
    {
        public string Name;
        public string Status;
        public int? Map;
        public int[] Vehicles;
        public bool? ApiOnly;
        public bool? Interactive;
        public bool? OffScreen;
        public int? Cluster;
        public DateTime? TimeOfDay;
        public Weather Weather;
    }

    public class Weather
    {
        public float? rain;
        public float? fog;
        public float? wetness;
        public float? cloudiness;
    }

    public class SimulationModule : BaseModule<Simulation, SimulationRequest, SimulationResponse>
    {
        InlineValidator<Simulation> startValidator = new InlineValidator<Simulation>();

        public SimulationModule() : base("simulations")
        {
            Init();

            //
            // TODO: Fix it for API Only Simulation
            //
            // addValidator.RuleFor(o => (int)o.Map).Must(BeValidMap).WithMessage("You must specify a valid map id");
            // editValidator.RuleFor(o => (int)o.Map).Must(BeValidMap).WithMessage("You must specify a valid map id");
            startValidator.RuleFor(o => o).Must(BeValidSim).WithMessage("Cannot start an invalid simulation");
        }

        protected override void Init()
        {
            base.Init();
            Start();
            Stop();
        }

        protected override void List()
        {
            Get("/", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int page = this.Request.Query["page"];

                        // 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                        int count = this.Request.Query["count"] > 0 ? this.Request.Query["count"] : 5;
                        var models = db.Page<Simulation>(page, count).Items;
                        Debug.Log($"Listing {ModulePath}");
                        return models.Select(m => CheckStatus(m)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to list {typeof(Simulation).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to list {typeof(Simulation).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected void Start()
        {
            Post("/{id}/start", x =>
            {
                try
                {
                    int id = x.id;
                    if (Loader.Instance.CurrentSimulation != null)
                    {
                        throw new Exception($"simulation with id {Loader.Instance.CurrentSimulation.Id} is already running");
                    }

                    using (var db = DatabaseManager.Open())
                    {
                        var model = db.Single<Simulation>(id);

                        ValidationResult startValidation = startValidator.Validate(model);
                        if (!startValidation.IsValid)
                        {
                            model.Status = "Invalid";
                            db.Update(model);

                            throw new Exception(startValidation.Errors.FirstOrDefault().ErrorMessage);
                        }

                        Debug.Log($"Starting simulation with id {id}");
                        Loader.StartAsync(model);
                    }
                    return HttpStatusCode.OK;
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to start {typeof(Simulation).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to start {typeof(Simulation).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start {typeof(Simulation).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to start {typeof(Simulation).ToString()}: {ex.Message}.",
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected void Stop()
        {
            Post("/{id}/stop", x =>
            {
                try
                {
                    int id = x.id;
                    if (Loader.Instance.CurrentSimulation != null && Loader.Instance.CurrentSimulation.Id != id)
                    {
                        throw new Exception($"Simulation with id {id} is not running");
                    }

                    var runningSimulation = Loader.Instance.CurrentSimulation;
                    Debug.Log($"Stopping simulation {runningSimulation.Name}");

                    Loader.StopAsync();

                    return HttpStatusCode.OK;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to stop {typeof(Simulation).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to stop {typeof(Simulation).ToString()}: {ex.Message}.",
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        private static Simulation CheckStatus(Simulation s)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Select("Count(*)").From("maps").Where("id = @0", s.Map).Where("status = @0", "Valid");
                var record = db.Single<int>(sql);

                s.Status = record == 1 ? "Valid" : "Invalid";

                if (s.Vehicles != null)
                {
                    sql = Sql.Builder.Select("Count(*)").From("vehicles").Where("id IN (@0)", s.Vehicles).Where("status = @0", "Valid");
                    
                    record = db.Single<int>(sql);
                    s.Status = record == s.Vehicles.Split(',').Length ? s.Status : "Invalid";
                }

                if (Loader.Instance.CurrentSimulation != null && s.Id == Loader.Instance.CurrentSimulation.Id)
                {
                    s.Status = "Running";
                }
            }

            return s;
        }

        private static bool BeValidSim(Simulation s)
        {
            return CheckStatus(s).Status == "Valid";
        }

        protected override Simulation ConvertToModel(SimulationRequest simRequest)
        {
            Simulation simulation = new Simulation();
            simulation.Name = simRequest.name;
            simulation.Map = simRequest.map;
            simulation.ApiOnly = simRequest.apiOnly;
            simulation.Interactive = simRequest.interactive;
            simulation.OffScreen = simRequest.offScreen;
            simulation.Cluster = simRequest.cluster;
            simulation.TimeOfDay = simRequest.timeOfDay;

            if (simRequest.vehicles != null && simRequest.vehicles.Length > 0)
            {
                simulation.Vehicles = string.Join(",", simRequest.vehicles.Select(x => x.ToString()));
            }

            if (simRequest.weather != null)
            {
                simulation.Rain = simRequest.weather.rain;
                simulation.Fog = simRequest.weather.fog;
                simulation.Cloudiness = simRequest.weather.cloudiness;
                simulation.Wetness = simRequest.weather.wetness;
            }

            return simulation;
        }

        public static SimulationResponse ConvertSimToResponse(Simulation simulation)
        {
            SimulationResponse simResponse = new SimulationResponse();
            simResponse.Name = simulation.Name;
            simResponse.Status = simulation.Status;
            simResponse.Map = simulation.Map;
            simResponse.ApiOnly = simulation.ApiOnly;
            simResponse.Interactive = simulation.Interactive;
            simResponse.OffScreen = simulation.OffScreen;
            simResponse.Cluster = simulation.Cluster;
            simResponse.Id = simulation.Id;

            if (simulation.Vehicles != null && simulation.Vehicles.Length > 0)
            {
                simResponse.Vehicles = simulation.Vehicles.Split(',').Select(x => Convert.ToInt32(x)).ToArray();
            }

            simResponse.TimeOfDay = simulation.TimeOfDay;

            simResponse.Weather = new Weather()
            {
                rain = simulation.Rain,
                fog = simulation.Fog,
                wetness = simulation.Wetness,
                cloudiness = simulation.Cloudiness
            };

            return simResponse;
        }

        public override SimulationResponse ConvertToResponse(Simulation simulation)
        {
            return ConvertSimToResponse(simulation);
        }
    }
}