/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Simulator.Database;
using Simulator.Database.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Simulator.Web.Modules
{
    public class VehicleRequest
    {
        public string name;
        public string url;
        public string sensors;
        public string bridgeType;

        public VehicleModel ToModel(string owner)
        {
            return new VehicleModel()
            {
                Owner = owner,
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
        public string Error;

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
                Error = vehicle.Error,
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
                .Must(Validation.BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle");

            RuleFor(req => req.bridgeType).Must(Validation.BeValidBridgeType).WithMessage("You must select an existing bridge type or choose no bridge.");
            RuleFor(req => req.sensors).Must(Validation.BeValidSensorConfig).WithMessage("You must provide a valid sensor configuration.");
        }
    }

    public class VehiclesModule : NancyModule
    {
        public VehiclesModule(IVehicleService service, IUserService userService, IDownloadService downloadService, INotificationService notificationService) : base("vehicles")
        {
            this.RequiresAuthentication();

            Get("/{id}/preview", x => HttpStatusCode.NotFound);

            Get("/", x =>
            {
                Debug.Log($"Listing vehicles");
                try
                {
                    string filter = Request.Query["filter"];
                    int offset = Request.Query["offset"];
                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: Vehicles, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : Config.DefaultPageSize;
                    return service.List(filter, offset, count, this.Context.CurrentUser.Identity.Name)
                        .Select(vehicle =>
                        {
                            if (vehicle.Status != "Downloading")
                            {
                                bool valid;
                                try
                                {
                                    valid = Validation.BeValidAssetBundle(vehicle.LocalPath);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                    valid = false;
                                }

                                if (!valid)
                                {
                                    vehicle.Status = "Invalid";
                                    vehicle.Error = "Missing or wrong Vehicle AssetBundle. Please check content website for updated bundle or rebuild the bundle.";
                                    // TODO: this should be more precise what exactly is wrong (file missing, wrong BundleFormat version, not a zip file, etc...)
                                }
                            }

                            return vehicle;
                        })
                        .Select(VehicleResponse.Create)
                        .ToArray();
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
                    var vehicle = service.Get(id, this.Context.CurrentUser.Identity.Name);
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

                    var vehicle = req.ToModel(this.Context.CurrentUser.Identity.Name);

                    var uri = new Uri(vehicle.Url);
                    if (uri.IsFile)
                    {
                        vehicle.Status = "Valid";
                        vehicle.LocalPath = uri.LocalPath;
                    }
                    else if (service.GetCountOfUrl(vehicle.Url) > 0)
                    {
                        List<VehicleModel> matchingModels = service.GetAllMatchingUrl(vehicle.Url);
                        vehicle.Status = matchingModels[0].Status;
                        vehicle.LocalPath = matchingModels[0].LocalPath;
                    }
                    else
                    {
                        vehicle.Status = "Downloading";
                        vehicle.LocalPath = WebUtilities.GenerateLocalPath("Vehicles");
                    }

                    long id = service.Add(vehicle);
                    Debug.Log($"Vehicle added with id {id}");
                    vehicle.Id = id;
                    SIM.LogWeb(SIM.Web.VehicleAddName, vehicle.Name);
                    SIM.LogWeb(SIM.Web.VehicleAddURL, vehicle.Url);
                    SIM.LogWeb(SIM.Web.VehicleAddBridgeType, vehicle.BridgeType);

                    if (!uri.IsFile && service.GetCountOfUrl(vehicle.Url) == 1)
                    {
                        SIM.LogWeb(SIM.Web.VehicleDownloadStart, vehicle.Name);
                        downloadService.AddDownload(
                            uri,
                            vehicle.LocalPath,
                            progress => notificationService.Send("VehicleDownload", new { vehicle.Id, progress }, vehicle.Owner),
                            (success, ex) =>
                            {
                                bool passesValidation = success && Validation.BeValidAssetBundle(vehicle.LocalPath);
                                string status = passesValidation ? "Valid" : "Invalid";

                                service.SetStatusForPath(status, vehicle.LocalPath);
                                service.GetAllMatchingUrl(vehicle.Url).ForEach(v =>
                                {
                                    if (!passesValidation)
                                    {
                                        v.Error = "You must specify a valid AssetBundle";
                                    }

                                    if (ex != null)
                                    {
                                        v.Error = ex.Message;
                                    }

                                    notificationService.Send("VehicleDownloadComplete", v, v.Owner);
                                    SIM.LogWeb(SIM.Web.VehicleDownloadFinish, vehicle.Name);
                                });
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


                    var vehicle = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    vehicle.Name = req.name;
                    vehicle.Sensors = req.sensors == null ? null : string.Join(",", req.sensors);
                    if (req.sensors != null)
                        SIM.LogWeb(SIM.Web.VehicleEditSensors);
                    vehicle.BridgeType = req.bridgeType;
                    SIM.LogWeb(SIM.Web.VehicleEditName, vehicle.Name);
                    SIM.LogWeb(SIM.Web.VehicleEditURL, vehicle.Url);
                    SIM.LogWeb(SIM.Web.VehicleEditBridgeType, vehicle.BridgeType);

                    if (vehicle.Url != req.url)
                    {
                        Uri uri = new Uri(req.url);
                        if (uri.IsFile)
                        {
                            vehicle.Status = "Valid";
                            vehicle.LocalPath = uri.LocalPath;
                        }
                        else if (service.GetCountOfUrl(req.url) == 0)
                        {
                            vehicle.Status = "Downloading";
                            vehicle.LocalPath = WebUtilities.GenerateLocalPath("Vehicles");
                            SIM.LogWeb(SIM.Web.VehicleDownloadStart, vehicle.Name);
                            downloadService.AddDownload(
                                uri,
                                vehicle.LocalPath,
                                progress => notificationService.Send("VehicleDownload", new { vehicle.Id, progress }, vehicle.Owner),
                                (success, ex) =>
                                {
                                    bool passesValidation = success && Validation.BeValidAssetBundle(vehicle.LocalPath);
                                    string status = passesValidation ? "Valid" : "Invalid";
                                    service.SetStatusForPath(status, vehicle.LocalPath);
                                    service.GetAllMatchingUrl(vehicle.Url).ForEach(v =>
                                    {
                                        if (!passesValidation)
                                        {
                                            v.Error = "You must specify a valid AssetBundle";
                                        }

                                        // TODO: We have a bug about flickering vehicles, is it because of that?
                                        if (ex != null)
                                        {
                                            v.Error = ex.Message;
                                        }

                                        notificationService.Send("VehicleDownloadComplete", v, v.Owner);

                                        SIM.LogWeb(SIM.Web.VehicleDownloadFinish, vehicle.Name);
                                    });
                                }
                            );
                        }
                        else
                        {
                            List<VehicleModel> vehicles = service.GetAllMatchingUrl(req.url);
                            vehicle.Status = vehicles[0].Status;
                            vehicle.LocalPath = vehicles[0].LocalPath;
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
                    VehicleModel vehicle = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    if (service.GetCountOfUrl(vehicle.Url) == 1)
                    {
                        if (vehicle.Status == "Downloading")
                        {
                            downloadService.StopDownload(vehicle.Url);
                            SIM.LogWeb(SIM.Web.VehicleDownloadStop, vehicle.Name);
                        }
                        if (!new Uri(vehicle.Url).IsFile && File.Exists(vehicle.LocalPath))
                        {
                            Debug.Log($"Deleting file at path: {vehicle.LocalPath}");
                            File.Delete(vehicle.LocalPath);
                        }
                    }

                    int result = service.Delete(id, vehicle.Owner);
                    SIM.LogWeb(SIM.Web.VehicleDeleteName, vehicle.Name);
                    SIM.LogWeb(SIM.Web.VehicleDeleteURL, vehicle.Url);
                    SIM.LogWeb(SIM.Web.VehicleDeleteBridgeType, vehicle.BridgeType);
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
                    VehicleModel vehicle = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    if (vehicle.Status == "Downloading")
                    {
                        downloadService.StopDownload(vehicle.Url);
                        vehicle.Status = "Invalid";
                        service.Update(vehicle);
                        SIM.LogWeb(SIM.Web.VehicleDownloadStop, vehicle.Name);
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
                    VehicleModel vehicle = service.Get(id, this.Context.CurrentUser.Identity.Name);
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
                                    notificationService.Send("VehicleDownload", new { vehicle.Id, progress }, vehicle.Owner);
                                },
                                (success, ex) =>
                                {
                                    var updatedModel = service.Get(id, vehicle.Owner);
                                    updatedModel.Status = success && Validation.BeValidAssetBundle(updatedModel.LocalPath) ? "Valid" : "Invalid";
                                    if (ex != null)
                                    {
                                        updatedModel.Error = ex.Message;
                                    }

                                    service.Update(updatedModel);
                                    notificationService.Send("VehicleDownloadComplete", updatedModel, updatedModel.Owner);

                                    SIM.LogWeb(SIM.Web.VehicleDownloadFinish, vehicle.Name);
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
