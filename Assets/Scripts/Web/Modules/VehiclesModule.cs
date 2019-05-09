/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Nancy;
using Nancy.ModelBinding;
using FluentValidation;
using Simulator.Database;
using Simulator.Database.Services;
using Web;

namespace Simulator.Web.Modules
{
    public class VehicleRequest
    {
        public string name;
        public string url;
        public string[] sensors;

        public Vehicle ToModel()
        {
            return new Vehicle()
            {
                Name = name,
                Url = url,
                Sensors = sensors == null ? string.Empty : string.Join(",", sensors),
            };
        }
    }

    public class VehicleResponse
    {
        public long Id;
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string Status;
        public string[] Sensors;

        public static VehicleResponse Create(Vehicle vehicle)
        {
            return new VehicleResponse()
            {
                Id = vehicle.Id,
                Name = vehicle.Name,
                Url = vehicle.Url,
                PreviewUrl = vehicle.PreviewUrl,
                Status = vehicle.Status,
                Sensors = string.IsNullOrEmpty(vehicle.Sensors) ? Array.Empty<string>() : vehicle.Sensors.Split(','),
            };
        }
    }

    public class VehicleRequestValidator : AbstractValidator<VehicleRequest>
    {
        public VehicleRequestValidator()
        {
            RuleFor(req => req.name)
                .NotEmpty().WithMessage("You must specify a non-empty name");

            RuleFor(req => req.url).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("You must specify a non-empty URL")
                .Must(Validation.IsValidUrl).WithMessage("You must specify a valid URL")
                .Must(Validation.BeValidFilePath).WithMessage("You must specify a valid URL")
                .Must(Validation.BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle File");
        }
    }

    public class VehiclesModule : NancyModule
    {
        public VehiclesModule(IVehicleService db) : base("vehicles")
        {
            Before += ctx =>
            {
                db.Open();
                return null;
            };
            After += ctx => db.Close();

            Get("/{id}/preview", x => HttpStatusCode.NotFound);

            Get("/", x =>
            {
                Debug.Log($"Listing vehicles");
                try
                {
                    int page = Request.Query["page"];

                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : 5;
                    return db.List(page, count).Select(VehicleResponse.Create).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to list vehicles: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Get("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Getting vehicle with id {id}");

                try
                {
                    var vehicle = db.Get(id);
                    return VehicleResponse.Create(vehicle);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Vehicle with id {id} does not exist");
                    return Response.AsJson(new { error = $"Vehicle with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to vehicle with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/", x =>
            {
                Debug.Log($"Adding new vehicle");
                try
                {
                    var req = this.BindAndValidate<VehicleRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for adding vehicle failed: {message}");
                        return Response.AsJson(new { error = $"Failed to add vehicle: {message}" }, HttpStatusCode.BadRequest);
                    }
                    var vehicle = req.ToModel();

                    var uri = new Uri(vehicle.Url);
                    if (uri.IsFile)
                    {
                        vehicle.Status = "Valid";
                        vehicle.LocalPath = uri.LocalPath;
                    }
                    else
                    {
                        vehicle.Status = "Downloading";
                        vehicle.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Vehicles", Path.GetFileName(uri.AbsolutePath));
                    }

                    long id = db.Add(vehicle);
                    Debug.Log($"Vehicle added with id {id}");
                    vehicle.Id = id;

                    if (!uri.IsFile)
                    {
                        DownloadManager.AddDownloadToQueue(new Download(uri, vehicle.LocalPath, (o, e) => VehicleDownloadComplete(id), (o, e) => VehicleDownloadUpdate(vehicle, e)));
                    }

                    return VehicleResponse.Create(vehicle);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to add vehicle: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Updating vehicle with id {id}");

                try
                {
                    var req = this.BindAndValidate<VehicleRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for updating vehicle failed: {message}");
                        return Response.AsJson(new { error = $"Failed to update vehicle: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var vehicle = db.Get(id);
                    vehicle.Name = req.name;

                    if (vehicle.Url != req.url)
                    {
                        Uri uri = new Uri(req.url);
                        if (uri.IsFile)
                        {
                            vehicle.Status = "Valid";
                            vehicle.LocalPath = uri.LocalPath;
                        }
                        else
                        {
                            vehicle.Status = "Downloading";
                            vehicle.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Vehicles", Path.GetFileName(uri.AbsolutePath));
                            DownloadManager.AddDownloadToQueue(new Download(uri, vehicle.LocalPath, (o, e) => VehicleDownloadComplete(id), (o, e) => VehicleDownloadUpdate(vehicle, e)));
                        }
                        vehicle.Url = req.url;
                    }

                    int result = db.Update(vehicle);
                    if (result > 1)
                    {
                        throw new Exception($"More than one vehicle has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return VehicleResponse.Create(vehicle);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Vehicle with id {id} does not exist");
                    return Response.AsJson(new { error = $"Vehicle with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to update vehicle with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });


            Delete("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Removing vehicle with id {id}");

                try
                {
                    int result = db.Delete(id);
                    if (result > 1)
                    {
                        throw new Exception($"More than one vehicle has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Vehicle with id {id} does not exist");
                    return Response.AsJson(new { error = $"Vehicle with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to remove vehicle with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });


        }

        private static void VehicleDownloadComplete(object id)
        {
            using (var database = DatabaseManager.Open())
            {
                Vehicle updatedModel = database.Single<Vehicle>(id);
                updatedModel.Status = "Valid";
                database.Update(updatedModel);
                NotificationManager.SendNotification("VehicleDownloadComplete", updatedModel);
            }
        }

        private static void VehicleDownloadUpdate(Vehicle Vehicle, System.Net.DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage != DownloadManager.currentPercentage)
            {
                DownloadManager.currentPercentage = e.ProgressPercentage;
                NotificationManager.SendNotification("VehicleDownload", new
                {
                    Vehicle,
                    progress = e.ProgressPercentage,
                });
            }
        }
    }
}
