using Database;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Web.Modules
{
    public class VehicleRequest
    {
        public string name;
        public string url;
        public string[] sensors;
    }

    public class VehicleResponse : WebResponse
    {
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string Status;
        public string[] Sensors;
    }

    public class VehiclesModule : BaseModule<Vehicle, VehicleRequest, VehicleResponse>
    {
        public VehiclesModule() : base("vehicles")
        {
            base.Init();

            addValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            addValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            addValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");

            editValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            editValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            editValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
        }

        protected override void Add()
        {
            Post($"/", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = this.Bind<VehicleRequest >();
                        var model = ConvertToModel(boundObj);

                        addValidator.ValidateAndThrow(model);


                        Uri uri = new Uri(model.Url);
                        if (uri.IsFile)
                        {
                            model.Status = "Valid";
                            model.LocalPath = uri.LocalPath;
                        }
                        else
                        {
                            model.Status = "Downloading";
                            model.LocalPath = Path.Combine(DownloadManager.dataPath, "..", Path.GetFileName(uri.AbsolutePath));
                        }

                        object id = db.Insert(model);

                        if (!uri.IsFile)
                        {
                            DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", Path.GetFileName(uri.AbsolutePath)), (o, e) =>
                            {
                                using (var database = DatabaseManager.Open())
                                {
                                    Vehicle updatedModel = db.Single<Vehicle>(id);
                                    updatedModel.Status = "Valid";
                                    db.Update(updatedModel);
                                }
                            }));
                        }

                        Debug.Log($"Adding {typeof(Vehicle).ToString()} with id {model.Id}");

                        return ConvertToResponse(model);
                    }
                }
                catch (ValidationException ex)
                {
                    Debug.Log($"Failed to add {typeof(Vehicle).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to add {typeof(Vehicle).ToString()}: {ex.Message}."
                    }, HttpStatusCode.BadRequest);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to add {typeof(Vehicle).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to add {typeof(Vehicle).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }


        protected override void Update()
        {
            Put("/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = this.Bind<VehicleRequest>();
                        Vehicle model = ConvertToModel(boundObj);
                        model.Id = x.id;

                        editValidator.ValidateAndThrow(model);
                        int id = x.id;
                        Vehicle originalModel = db.Single<Vehicle>(id);

                        model.Status = "Valid";

                        if (model.LocalPath != originalModel.LocalPath)
                        {
                            Uri uri = new Uri(model.Url);
                            if (uri.IsFile)
                            {
                                model.LocalPath = uri.LocalPath;
                            }
                            else
                            {
                                model.Status = "Downloading";
                                model.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Vehicles", Path.GetFileName(uri.AbsolutePath));
                                DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Vehicles", Path.GetFileName(uri.AbsolutePath)), (o, e) =>
                                {
                                    using (var database = DatabaseManager.Open())
                                    {
                                        Vehicle updatedModel = db.Single<Vehicle>(id);
                                        updatedModel.Status = "Valid";
                                        db.Update(updatedModel);
                                    }
                                }));
                            }
                        }

                        int result = db.Update(model);
                        if (result > 1)
                        {
                            throw new Exception($"more than one object has id {model.Id}");
                        }

                        if (result < 1)
                        {
                            throw new IndexOutOfRangeException($"id {x.id} does not exist");
                        }

                        Debug.Log($"Updating {typeof(Vehicle).ToString()} with id {model.Id}");
                        return ConvertToResponse(model);
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to update {typeof(Map).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to update {typeof(Map).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to update {typeof(Map).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to update {typeof(Map).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
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

            vehicle.Status = "1";
            return vehicle;
        }

        public override VehicleResponse ConvertToResponse(Vehicle vehicle)
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