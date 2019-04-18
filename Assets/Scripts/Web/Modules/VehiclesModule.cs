using System.Linq;
using Database;
using FluentValidation;

namespace Web.Modules
{
    public class VehiclesModule : BaseModule<Vehicle, VehicleRequest, VehicleResponse>
    {
        public VehiclesModule()
        {
            header = "vehicles";
            base.Init();

            addValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            addValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            addValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");

            editValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            editValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            editValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
        }

        protected override Vehicle ConvertToModel(VehicleRequest vehicleRequest)
        {
            Vehicle vehicle = new Vehicle();
            vehicle.Name = vehicleRequest.name;
            vehicle.Url = vehicleRequest.url;
            if (vehicleRequest.sensors != null && vehicleRequest.sensors.Length > 0)
            {
                vehicle.Sensors = string.Join(",", vehicleRequest.sensors.Select(x => x.ToString()).ToArray());
            }

            vehicle.Status =  "1";
            return vehicle;
        }

        protected override VehicleResponse ConvertToResponse(Vehicle vehicle)
        {
            VehicleResponse vehicleResponse = new VehicleResponse();
            vehicleResponse.Id = vehicle.Id;
            vehicleResponse.Name = vehicle.Name;
            vehicleResponse.Url = vehicle.Url;
            vehicleResponse.PreviewUrl = vehicle.PreviewUrl;
            vehicleResponse.Status = vehicle.Status;
            if (vehicle.Sensors != null && vehicle.Sensors.Length > 0)
            {
                vehicleResponse.Sensors = vehicle.Sensors.Split(',');
            }

            return vehicleResponse;
        }
    }
}