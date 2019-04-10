using System;

using Database;
using Database.Models;
using FluentValidation;
using Nancy.ModelBinding;

namespace Web.Modules
{
    public class SimulationModule : BaseModule<Simulation>
    {
        InlineValidator<Simulation> startValidator = new InlineValidator<Simulation>();

        public SimulationModule()
        {
            header = "simulations";
            Init();

            addValidator.RuleFor(o => o.Map).Must(BeValidMap).WithMessage("You must specify a valid Map Id");

            editValidator.RuleFor(o => o.Map).Must(BeValidMap).WithMessage("You must specify a valid Map Id");

            startValidator.RuleFor(o => o.Map).Must(BeValidMap).WithMessage("You must specify a valid Map Id");
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
                    var boundObj = DatabaseManager.db.Single<Simulation>(id);
                    startValidator.ValidateAndThrow(boundObj);
                    BundleManager.instance.Load(DatabaseManager.db.Single<Map>(boundObj.Map).Url);
                    // TODO: initiate download boundObj here if needed
                    // ...
                    return new
                    {
                        status = "success"
                    };
                }
                catch (Exception ex)
                {
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
            return DatabaseManager.db.SingleOrDefault<Map>(mapId) != null;
        }

        protected static bool BeValidVehicles(string vehicleIds)
        {
            string[] ids = vehicleIds.Split(',');
            for (int i = 0; i < ids.Length; i++)
            {
                if (DatabaseManager.db.SingleOrDefault<Vehicle>(Convert.ToInt32(ids[i])) == null) return false;
            }

            return true;
        }
    }
}