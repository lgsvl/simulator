using System;
using System.Linq;
using UnityEngine;

using Database;
using FluentValidation;

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

        public SimulationModule()
        {
            header = "simulations";
            Init();

            addValidator.RuleFor(o => (int)o.Map).Must(BeValidMap).WithMessage("You must specify a valid map id");

            editValidator.RuleFor(o => (int)o.Map).Must(BeValidMap).WithMessage("You must specify a valid map id");

            startValidator.RuleFor(o => (int)o.Map).Must(BeValidMap).WithMessage("You must specify a valid map id");
        }

        protected override void Init()
        {
            base.Init();
            Start();
        }

        protected void Start()
        {
            Post("/simulations/{id}/start", x =>
            {
                try
                {
                    int id = x.id;
                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = db.Single<Simulation>(id);

                        startValidator.ValidateAndThrow(boundObj);

                        Debug.Log($"Starting simulation {boundObj.Name}");

                        foreach (string vehicleID in boundObj.Vehicles.Split(','))
                        {
                            BundleManager.instance.Load(new Uri(db.Single<Vehicle>(Convert.ToInt32(vehicleID)).Url).LocalPath);
                        }

                        BundleManager.instance.Load(new Uri(db.Single<Map>(boundObj.Map).Url).LocalPath);
                        // TODO: initiate download boundObj here if needed
                        // ...
                    }

                    return new
                    {
                        status = "success",
                    };
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to add {typeof(Simulation).ToString()}: {ex.Message}.");
                    return new
                    {
                        status = "error",
                        error = $"Failed to add {typeof(Simulation).ToString()}: {ex.Message}.",
                    };
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

        protected override SimulationResponse ConvertToResponse(Simulation simulation)
        {
            SimulationResponse simResponse = new SimulationResponse();
            simResponse.Name = simulation.Name;
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
    }
}