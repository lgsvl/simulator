using System;
using System.Linq;
using UnityEngine;

using Database;
using FluentValidation;
using FluentValidation.Results;
using Nancy;
using System.Threading.Tasks;

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
            // startValidator.RuleFor(o => (int)o.Map).Must(BeValidMap).WithMessage("You must specify a valid map id");
        }

        protected override void Init()
        {
            base.Init();
            Start();
            Stop();
        }

        protected void Start()
        {
            Post("/{id}/start", x =>
            {
                try
                {
                    int id = x.id;
                    if (id == MainMenu.currentRunningId)
                    {
                        throw new Exception($"simulation with id {id} is already running");
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

                        StartSimulation(id);

                        model.Status = "Starting";
                        NotificationManager.SendNotification(new ClientMessage("simulation", ConvertToResponse(model)));
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
                    if (id != MainMenu.currentRunningId)
                    {
                        throw new Exception($"simulation with id {id} is not running");
                    }

                    using (var db = DatabaseManager.Open())
                    {
                        var runningSimulation = db.SingleOrDefault<Simulation>(MainMenu.currentRunningId);

                        Debug.Log($"Stopping simulation {runningSimulation.Name}");

                        runningSimulation.Status = "Stopping";
                        NotificationManager.SendNotification(new ClientMessage("simulation", SimulationModule.ConvertSimToResponse(runningSimulation)));

                        StopSimulation(id);
                    }

                    return HttpStatusCode.OK;
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to stop {typeof(Simulation).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to stop {typeof(Simulation).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
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

        private void StartSimulation(int id)
        {
            Debug.Log("Starting a new thread for simulation");
            Task.Run(() =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var simulation = db.Single<Simulation>(id);
                        try
                        {
                            // TODO: Replace with actual code to start simulation
                            //       we can block here till everything is ready
                            Task.Delay(2000).Wait();

                            // NOTE: Here we suppose to create Simulation object responsible for loading scene asynchronously
                            //       and store model.Id inside Simulation object.
                            //BundleManager.instance.Load(new Uri(db.Single<Map>(simulation.Map).Url).LocalPath);

                            // NOTE: After asynchronous scene loading is done we are loading vehicles asynchronously (in parallel?)
                            //foreach (string vehicleID in model.Vehicles.Split(','))
                            //{
                            //    BundleManager.instance.Load(new Uri(db.Single<Vehicle>(Convert.ToInt32(vehicleID)).Url).LocalPath);
                            //}

                            MainMenu.currentRunningId = id;
                            simulation.Status = "Running";
                            NotificationManager.SendNotification(new ClientMessage("simulation", ConvertToResponse(simulation)));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);

                            // NOTE: In case of failure we have to update Simulation state
                            simulation.Status = "Invalid";
                            db.Update(simulation);

                            // TODO: take ex.Message and append it to response here
                            NotificationManager.SendNotification(new ClientMessage("simulation", ConvertToResponse(simulation)));
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start simulation with {id}");
                    Debug.LogException(ex);
                    // TODO: We need to send HTTP notification here about failed simulation
                    //       There is no complete simulation object available, only ID
                }
            });
        }

        private void StopSimulation(int id)
        {
            Debug.Log("Starting a new thread to stop simulation");
            Task.Run(() =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var runningSimulation = db.Single<Simulation>(id);
                        try
                        {
                            // TODO: Replace with actual code to stop simulation
                            //       we can block here till everything is ready
                            Task.Delay(2000).Wait();

                            MainMenu.currentRunningId = -1;
                            runningSimulation.Status = "Valid";
                            NotificationManager.SendNotification(new ClientMessage("simulation", SimulationModule.ConvertSimToResponse(runningSimulation)));
                            Debug.Log($"Simulation with id {id} stopped successfully");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            runningSimulation.Status = "Invalid";
                            db.Update(runningSimulation);

                            // TODO: take ex.Message and append it to response here
                            NotificationManager.SendNotification(new ClientMessage("simulation", ConvertToResponse(runningSimulation)));
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to stop simulation with {id}");
                    Debug.LogException(ex);

                    // TODO: We need to send HTTP notification here about failed simulation
                    //       There is no complete simulation object available, only ID
                }
            });
        }

        protected static bool BeValidMap(int mapId)
        {
            Map map = DatabaseManager.CurrentDb.SingleOrDefault<Map>(mapId);
            if(map == null)
            {
                Debug.Log($"Faild map validation: there is no map with id {mapId}");
            }

            return map != null;
        }

        protected static bool BeValidVehicles(string vehicleIds)
        {
            string[] ids = vehicleIds.Split(',');

            for (int i = 0; i < ids.Length; i++)
            {
                if (DatabaseManager.CurrentDb.SingleOrDefault<Vehicle>(Convert.ToInt32(ids[i])) == null)
                {
                    Debug.Log($"Failed vehicle validation: there is no vehicle with id {ids[i]}");
                    return false;
                }
            }

            return true;
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