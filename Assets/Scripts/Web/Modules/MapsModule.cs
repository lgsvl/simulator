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
using System.IO;
using System.Linq;
using UnityEngine;

namespace Simulator.Web.Modules
{
    public class MapRequest
    {
        public string name;
        public string url;

        public MapModel ToModel(string owner)
        {
            return new MapModel()
            {
                Owner = owner,
                Name = name,
                Url = url,
            };
        }
    }

    public class MapResponse
    {
        public long Id;
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string Status;
        public string Error;

        public static MapResponse Create(MapModel map)
        {
            return new MapResponse()
            {
                Name = map.Name,
                Url = map.Url,
                PreviewUrl = map.PreviewUrl,
                Status = map.Status,
                Id = (long)map.Id,
                Error = map.Error,
            };
        }
    }

    public class MapRequestValidator : AbstractValidator<MapRequest>
    {
        public MapRequestValidator()
        {
            RuleFor(req => req.name)
                .NotEmpty().WithMessage("You must specify a non-empty name");

            RuleFor(req => req.url).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("You must specify a non-empty URL")
                .Must(Validation.IsValidUrl).WithMessage("You must specify a valid URL")
                .Must(Validation.BeValidFilePath).WithMessage("You must specify a valid URL")
                .Must(Validation.BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle");
        }
    }

    public class MapsModule : NancyModule
    {
        public MapsModule(IMapService service, IDownloadService downloadService, INotificationService notificationService) : base("maps")
        {
            this.RequiresAuthentication();

            Get("/{id}/preview", x => HttpStatusCode.NotFound);

            Get("/", x =>
            {
                Debug.Log($"Listing maps");
                try
                {
                    string filter = Request.Query["filter"];
                    int offset = Request.Query["offset"];
                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : Config.DefaultPageSize;
                    return service.List(filter, offset, count, this.Context.CurrentUser.Identity.Name)
                        .Select(map =>
                        {
                            if (map.Status != "Downloading")
                            {
                                bool valid;
                                try
                                {
                                    valid = Validation.BeValidAssetBundle(map.LocalPath);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                    valid = false;
                                }

                                if (!valid)
                                {
                                    map.Status = "Invalid";
                                    map.Error = "Missing or wrong Map AssetBundle. Please check content website for updated bundle or rebuild the bundle.";
                                    // TODO: this should be more precise what exactly is wrong (file missing, wrong BundleFormat version, not a zip file, etc...)
                                }
                            }
                            return map;
                        })
                        .Select(MapResponse.Create)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to list maps: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Get("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Getting map with id {id}");
                try
                {
                    var map = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    return MapResponse.Create(map);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Map with id {id} does not exist");
                    return Response.AsJson(new { error = $"Map with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to get map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Post("/", x =>
            {
                Debug.Log($"Adding new map");
                try
                {
                    var req = this.BindAndValidate<MapRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for adding map failed: {message}");
                        return Response.AsJson(new { error = $"Failed to add map: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var map = req.ToModel(this.Context.CurrentUser.Identity.Name);
                    string localPath = service.GetExistingLocalPath(map.Url);
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        map.LocalPath = localPath;
                    }

                    var uri = new Uri(map.Url);
                    if (uri.IsFile)
                    {
                        map.Status = "Valid";
                        map.LocalPath = uri.LocalPath;
                    }
                    else
                    {
                        map.Status = "Downloading";
                        map.LocalPath = WebUtilities.GenerateLocalPath("Maps");
                    }

                    long id = service.Add(map);
                    Debug.Log($"Map added with id {id}");
                    map.Id = id;
                    SIM.LogWeb(SIM.Web.MapAddName, map.Name);
                    SIM.LogWeb(SIM.Web.MapAddURL, map.Url);

                    if (!uri.IsFile)
                    {
                        SIM.LogWeb(SIM.Web.MapDownloadStart, map.Name);
                        downloadService.AddDownload(
                            uri,
                            map.LocalPath,
                            progress => notificationService.Send("MapDownload", new { map.Id, progress }, map.Owner),
                            (success, ex) =>
                            {
                                var updatedModel = service.Get(id, map.Owner);

                                bool passesValidation = false;
                                if (success)
                                {
                                    passesValidation = Validation.BeValidAssetBundle(updatedModel.LocalPath);
                                    if (!passesValidation)
                                    {
                                        updatedModel.Error = "You must specify a valid AssetBundle";
                                    }
                                }

                                updatedModel.Status = passesValidation ? "Valid" : "Invalid";

                                if (ex != null)
                                {
                                    map.Error = ex.Message;
                                }

                                service.Update(updatedModel);
                                notificationService.Send("MapDownloadComplete", updatedModel, map.Owner);

                                SIM.LogWeb(SIM.Web.MapDownloadFinish, map.Name);
                            }
                        );
                    }

                    return MapResponse.Create(map);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to add map: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Updating map with id {id}");
                try
                {
                    var req = this.BindAndValidate<MapRequest>();
                    if (!ModelValidationResult.IsValid)
                    {
                        var message = ModelValidationResult.Errors.First().Value.First().ErrorMessage;
                        Debug.Log($"Validation for updating map failed: {message}");
                        return Response.AsJson(new { error = $"Failed to update map: {message}" }, HttpStatusCode.BadRequest);
                    }

                    var map = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    map.Name = req.name;

                    if (map.Url != req.url)
                    {
                        Uri uri = new Uri(req.url);
                        if (uri.IsFile)
                        {
                            map.Status = "Valid";
                            map.LocalPath = uri.LocalPath;
                        }
                        else if(service.GetCountOfLocal(map.Url) == 0)
                        {
                            map.Status = "Downloading";
                            map.LocalPath = WebUtilities.GenerateLocalPath("Maps");
                            SIM.LogWeb(SIM.Web.MapDownloadStart, map.Name);
                            downloadService.AddDownload(
                                uri,
                                map.LocalPath,
                                progress => notificationService.Send("MapDownload", new { map.Id, progress }, map.Owner),
                                (success, ex) =>
                                {
                                    var updatedModel = service.Get(id, map.Owner);
                                    bool passesValidation = false;
                                    if (success)
                                    {
                                        passesValidation = Validation.BeValidAssetBundle(updatedModel.LocalPath);
                                        if (!passesValidation)
                                        {
                                            updatedModel.Error = "You must specify a valid AssetBundle";
                                        }
                                    }

                                    updatedModel.Status = passesValidation ? "Valid" : "Invalid";

                                    if (ex != null)
                                    {
                                        updatedModel.Error = ex.Message;
                                    }

                                    service.Update(updatedModel);
                                    notificationService.Send("MapDownloadComplete", updatedModel, map.Owner);

                                    SIM.LogWeb(SIM.Web.MapDownloadFinish, map.Name);
                                }
                            );
                        }
                        else
                        {
                            string localPath = service.GetExistingLocalPath(map.Url);
                            map.Status = "Valid";
                            map.LocalPath = localPath;
                        }

                        map.Url = req.url;
                    }

                    int result = service.Update(map);
                    SIM.LogWeb(SIM.Web.MapEditName, map.Name);
                    SIM.LogWeb(SIM.Web.MapEditURL, map.Url);
                    if (result > 1)
                    {
                        throw new Exception($"More than one map has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return MapResponse.Create(map);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Map with id {id} does not exist");
                    return Response.AsJson(new { error = $"Map with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to update map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Delete("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Removing map with id {id}");
                try
                {
                    MapModel map = service.Get(id, this.Context.CurrentUser.Identity.Name);

                    if (service.GetCountOfLocal(map.LocalPath) == 1)
                    {
                        if (map.Status == "Downloading")
                        {
                            downloadService.StopDownload(map.Url);
                            SIM.LogWeb(SIM.Web.MapDownloadStop, map.Name);
                        }

                        if (!new Uri(map.Url).IsFile && File.Exists(map.LocalPath))
                        {
                            Debug.Log($"Deleting file at path: {map.LocalPath}");
                            File.Delete(map.LocalPath);
                        }
                    }

                    int result = service.Delete(id, map.Owner);
                    SIM.LogWeb(SIM.Web.MapDeleteName, map.Name);
                    SIM.LogWeb(SIM.Web.MapDeleteURL, map.Url);
                    if (result > 1)
                    {
                        throw new Exception($"More than one map has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Map with id {id} does not exist");
                    return Response.AsJson(new { error = $"Map with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to remove map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}/cancel", x =>
            {
                long id = x.id;
                Debug.Log($"Cancelling download of map with id {id}");
                try
                {
                    MapModel map = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    if (map.Status == "Downloading")
                    {
                        downloadService.StopDownload(map.Url);
                        map.Status = "Invalid";
                        service.Update(map);
                        SIM.LogWeb(SIM.Web.MapDownloadStop, map.Name);
                    }
                    else
                    {
                        throw new Exception($"Failed to cancel map download: map with id {id} is not currently downloading");
                    }

                    return new { };
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Map with id {id} does not exist");
                    return Response.AsJson(new { error = $"Map with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to cancel download of map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Put("/{id:long}/download", x =>
            {
                long id = x.id;
                Debug.Log($"Restarting download of map with id {id}");
                try
                {
                    MapModel map = service.Get(id, this.Context.CurrentUser.Identity.Name);
                    Uri uri = new Uri(map.Url);
                    if (!uri.IsFile)
                    {
                        if (map.Status == "Invalid")
                        {
                            map.Status = "Downloading";
                            downloadService.AddDownload(
                                uri,
                                map.LocalPath,
                                progress =>
                                {
                                    Debug.Log($"Map Download at {progress}%");
                                    notificationService.Send("MapDownload", new { map.Id, progress }, map.Owner);
                                },
                                (success, ex) =>
                                {
                                    var updatedModel = service.Get(id, map.Owner);
                                    updatedModel.Status = success && Validation.BeValidAssetBundle(updatedModel.LocalPath) ? "Valid" : "Invalid";
                                    if (ex != null)
                                    {
                                        updatedModel.Error = ex.Message;
                                    }

                                    service.Update(updatedModel);
                                    notificationService.Send("MapDownloadComplete", updatedModel, map.Owner);

                                    SIM.LogWeb(SIM.Web.MapDownloadFinish, map.Name);
                                }
                            );
                        }
                        else
                        {
                            throw new Exception($"Failed to restart download of map: map is not in invalid state");
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to restart download of map: file URL is not remote");
                    }

                    int result = service.Update(map);
                    if (result > 1)
                    {
                        throw new Exception($"More than one map has id {id}");
                    }
                    else if (result < 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return MapResponse.Create(map);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log($"Map with id {id} does not exist");
                    return Response.AsJson(new { error = $"Map with id {id} does not exist" }, HttpStatusCode.NotFound);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return Response.AsJson(new { error = $"Failed to cancel download of map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });
        }
    }
}
