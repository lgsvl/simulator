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

namespace Simulator.Web.Modules
{
    public class VehicleRequest
    {
        public string name;
        public string url;
        public string sensors;
        public string bridgeType;

        public VehicleModel ToModel()
        {
            return new VehicleModel()
            {
                Name = name,
                Url = url,
                BridgeType = bridgeType,
                Sensors = sensors,
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
        public string Sensors;
        public string BridgeType;

        public static VehicleResponse Create(VehicleModel vehicle)
        {
            return new VehicleResponse()
            {
                Id = vehicle.Id,
                Name = vehicle.Name,
                Url = vehicle.Url,
                BridgeType = vehicle.BridgeType,
                PreviewUrl = vehicle.PreviewUrl,
                Status = vehicle.Status,
                Sensors = vehicle.Sensors,
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

            RuleFor(req => req.bridgeType).Must(Validation.BeValidBridgeType).WithMessage("You must select an existing bridge type or choose no bridge.");
            RuleFor(req => req.sensors).Must(Validation.BeValidSensorConfig).WithMessage("You must provide a valid sensor configuration.");
        }
    }

    public class VehiclesModule : NancyModule
    {
        public VehiclesModule(IVehicleService service, IDownloadService downloadService, INotificationService notificationService) : base("vehicles")
        {
            Get("/{id}/preview", x => HttpStatusCode.NotFound);

            Get("/", x =>
            {
                Debug.Log($"Listing vehicles");
                try
                {
                    int page = Request.Query["page"];

                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: Vehicles, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : 5;
                    return service.List(page, count).Select(VehicleResponse.Create).ToArray();
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
                    var vehicle = service.Get(id);
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
                    return Response.AsJson(new { error = $"Failed to get vehicle with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
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
                        vehicle.LocalPath = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));
                    }

                    long id = service.Add(vehicle);
                    Debug.Log($"Vehicle added with id {id}");
                    vehicle.Id = id;

                    if (!uri.IsFile)
                    {
                        downloadService.AddDownload(
                            uri,
                            vehicle.LocalPath,
                            progress => notificationService.Send("VehicleDownload", new { vehicle.Id, progress }),
                            success =>
                            {
                                var updatedModel = service.Get(id);
                                updatedModel.Status = success && Validation.BeValidAssetBundle(updatedModel.LocalPath) ? "Valid" : "Invalid";
                                service.Update(updatedModel);
                                notificationService.Send("VehicleDownloadComplete", updatedModel);
                            }
                        );
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

                    var vehicle = service.Get(id);
                    vehicle.Name = req.name;
                    vehicle.Sensors = req.sensors == null ? null : string.Join(",", req.sensors);
                    vehicle.BridgeType = req.bridgeType;
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
                            vehicle.LocalPath = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));
                            downloadService.AddDownload(
                                uri,
                                vehicle.LocalPath,
                                progress => notificationService.Send("VehicleDownload", new { vehicle.Id, progress }),
                                success =>
                                {
                                    var updatedModel = service.Get(id);
                                    updatedModel.Status = success && Validation.BeValidAssetBundle(updatedModel.LocalPath) ? "Valid" : "Invalid";
                                    service.Update(updatedModel);
                                    notificationService.Send("VehicleDownloadComplete", updatedModel);
                                }
                            );
                        }
                        vehicle.Url = req.url;
                    }

                    int result = service.Update(vehicle);
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

                    VehicleModel vehicle = service.Get(id);
                    if (vehicle.Status == "Downloading")
                    {
                        downloadService.StopDownload(vehicle.Url);
                    }
                    if (service.GetCountOfLocal(vehicle.LocalPath) == 1 && File.Exists(vehicle.LocalPath))
                    {
                        Debug.Log($"Deleting file at path: {vehicle.LocalPath}");
                        File.Delete(vehicle.LocalPath);
                    }

                    int result = service.Delete(id);
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

            Put("/{id:long}/cancel", x =>
            {
                long id = x.id;
                Debug.Log($"Cancelling download of Vehicle with id {id}");
                try
                {
                    VehicleModel vehicle = service.Get(id);
                    if (vehicle.Status == "Downloading")
                    {
                        downloadService.StopDownload(vehicle.Url);
                        vehicle.Status = "Invalid";
                        service.Update(vehicle);
                    }
                    else
                    {
                        throw new Exception($"Failed to cancel Vehicle download: Vehicle with id {id} is not currently downloading");
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
                    return Response.AsJson(new { error = $"Failed to cancel download of vehicle with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}/download", x =>
            {
                long id = x.id;
                Debug.Log($"Restarting download of Vehicle with id {id}");
                try
                {
                    VehicleModel vehicle = service.Get(id);
                    Uri uri = new Uri(vehicle.Url);
                    if (!uri.IsFile)
                    {
                        if (vehicle.Status == "Invalid")
                        {
                            vehicle.Status = "Downloading";
                            downloadService.AddDownload(
                                uri,
                                vehicle.LocalPath,
                                progress =>
                                {
                                    Debug.Log($"Vehicle Download at {progress}%");
                                    notificationService.Send("VehicleDownload", new { vehicle.Id, progress });
                                },
                                success =>
                                {
                                    var updatedModel = service.Get(id);
                                    updatedModel.Status = success && Validation.BeValidAssetBundle(updatedModel.LocalPath) ? "Valid" : "Invalid";
                                    service.Update(updatedModel);
                                    notificationService.Send("VehicleDownloadComplete", updatedModel);
                                }
                            );
                        }
                        else
                        {
                            throw new Exception($"Failed to restart download of Vehicle: Vehicle is not in invalid state");
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to restart download of Vehicle: file URL is not remote");
                    }

                    int result = service.Update(vehicle);
                    if (result > 1)
                    {
                        throw new Exception($"More than one Vehicle has id {id}");
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
                    return Response.AsJson(new { error = $"Failed to cancel download of Vehicle with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });
        }
    }
}
