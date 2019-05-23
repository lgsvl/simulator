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
using System.ComponentModel;

namespace Simulator.Web.Modules
{
    public class MapRequest
    {
        public string name;
        public string url;

        public MapModel ToModel()
        {
            return new MapModel()
            {
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

        public static MapResponse Create(MapModel map)
        {
            return new MapResponse()
            {
                Name = map.Name,
                Url = map.Url,
                PreviewUrl = map.PreviewUrl,
                Status = map.Status,
                Id = map.Id,
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
                .Must(Validation.BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle File");
        }
    }

    public class MapsModule : NancyModule
    {
        public MapsModule(IMapService service, IDownloadService downloadService, INotificationService notificationService) : base("maps")
        {
            Get("/{id}/preview", x => HttpStatusCode.NotFound);

            Get("/", x =>
            {
                Debug.Log($"Listing maps");
                try
                {
                    int page = Request.Query["page"];

                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : 5;
                    return service.List(page, count).Select(MapResponse.Create).ToArray();
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
                    var map = service.Get(id);
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
                    var map = req.ToModel();

                    var uri = new Uri(map.Url);
                    if (uri.IsFile)
                    {
                        map.Status = "Valid";
                        map.LocalPath = uri.LocalPath;
                    }
                    else
                    {
                        map.Status = "Downloading";
                        map.LocalPath = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));
                    }

                    long id = service.Add(map);
                    Debug.Log($"Map added with id {id}");
                    map.Id = id;

                    if (!uri.IsFile)
                    {
                        downloadService.AddDownload(
                            uri,
                            map.LocalPath,
                            progress => notificationService.Send("MapDownload", new { progress }),
                            success =>
                            {
                                var updatedModel = service.Get(id);
                                updatedModel.Status = success ? "Valid" : "Invalid";
                                service.Update(updatedModel);
                                notificationService.Send("MapDownloadComplete", updatedModel);
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

                    var map = service.Get(id);
                    map.Name = req.name;

                    if (map.Url != req.url)
                    {
                        Uri uri = new Uri(req.url);
                        if (uri.IsFile)
                        {
                            map.Status = "Valid";
                            map.LocalPath = uri.LocalPath;
                        }
                        else
                        {
                            map.Status = "Downloading";
                            map.LocalPath = Path.Combine(Config.PersistentDataPath, Path.GetFileName(uri.AbsolutePath));

                            downloadService.AddDownload(
                                uri,
                                map.LocalPath,
                                progress => notificationService.Send("MapDownload", new { progress }),
                                success =>
                                {
                                    var updatedModel = service.Get(id);
                                    updatedModel.Status = success ? "Valid" : "Invalid";
                                    service.Update(updatedModel);
                                    notificationService.Send("MapDownloadComplete", updatedModel);
                                }
                            );
                        }
                        map.Url = req.url;
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
                    return Response.AsJson(new { error = $"Failed to update map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
                }
            });

            Delete("/{id:long}", x =>
            {
                long id = x.id;
                Debug.Log($"Removing map with id {id}");

                try
                {
                    MapModel map = service.Get(id);
                    if (File.Exists(map.LocalPath))
                    {
                        Debug.Log($"Deleting file at path: {map.LocalPath}");
                        File.Delete(map.LocalPath);
                    }

                    int result = service.Delete(id);
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
                    MapModel map = service.Get(id);
                    if (map.Status == "Downloading")
                    {
                        downloadService.StopDownload();
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
                    MapModel map = service.Get(id);
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
                                    notificationService.Send("MapDownload", new { progress });
                                },
                                success =>
                                {
                                    var updatedModel = service.Get(id);
                                    updatedModel.Status = success ? "Valid" : "Invalid";
                                    service.Update(updatedModel);
                                    notificationService.Send("MapDownloadComplete", updatedModel);
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
