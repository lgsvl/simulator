/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Nancy;
using Nancy.ModelBinding;
using FluentValidation;
using Simulator.Database;
using Simulator.Database.Services;
using Web;

namespace Simulator.Web.Modules
{
    public class MapRequest
    {
        public string name;
        public string url;

        public Map ToModel()
        {
            return new Map()
            {
                Name = name,
                Url = url,
                Status = "Valid",
            };
        }
    }

    public class MapResponse
    {
        public long Id;
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string LocalPath;
        public string Status;

        public static MapResponse Create(Map map)
        {
            return new MapResponse()
            {
                Name = map.Name,
                Url = map.Url,
                LocalPath = map.LocalPath,
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
                .Must(IsValidUrl).WithMessage("You must specify a valid URL")
                .Must(BeValidFilePath).WithMessage("You must specify a valid URL")
                .Must(BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle File");
        }

        static bool IsValidUrl(string url)
        {
            try
            {
                new Uri(url);
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        // TODO:
        // set responseStatus
        // if url is new:
        // if url starts with file:// and file exists set localPath, if it does not exist throw an exception
        // if url starts with http:// or https:// create a temporary file and initiate downloading
        //    when downloading is completed move file to expected location and update localPath
        // otherwise throw exception with error message
        static bool BeValidFilePath(string url)
        {
            var uri = new Uri(url);
            if (uri.IsFile)
            {
                return File.Exists(uri.LocalPath);
            }
            else
            {
                return uri.IsWellFormedOriginalString();
            }
        }

        // NOTE: Let's rename to BeValidAssetBundle and
        //       check that file content starts with 'UnityFS'
        // Open bundle read first 7 bytes
        static bool BeValidAssetBundle(string url)
        {
            var uri = new Uri(url);
            byte[] buffer = new byte[7];
            try
            {
                if (uri.IsFile)
                {
                    using (var fs = new FileStream(uri.AbsolutePath, FileMode.Open, FileAccess.Read))
                    {
                        if (fs.Read(buffer, 0, buffer.Length) != buffer.Length)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // TODO: check remote file
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogException(ex);
                return false;
            }

            return Encoding.ASCII.GetString(buffer) == "UnityFS";
        }
    }

    public class MapsModule : NancyModule
    {
        public MapsModule(IMapService db) : base("maps")
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
                Debug.Log($"Listing maps");
                try
                {
                    int page = Request.Query["page"];

                    // TODO: Items per page should be read from personal user settings.
                    //       This value should be independent for each module: maps, vehicles and simulation.
                    //       But for now 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                    int count = Request.Query["count"] > 0 ? Request.Query["count"] : 5;
                    return db.List(page, count).Select(MapResponse.Create).ToArray();
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
                    var map = db.Get(id);
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
                    return Response.AsJson(new { error = $"Failed to map with id {id}: {ex.Message}" }, HttpStatusCode.InternalServerError);
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
                        map.LocalPath = Path.Combine(DownloadManager.dataPath, "..", Path.GetFileName(uri.AbsolutePath));
                    }

                    long id = db.Add(map);
                    Debug.Log($"Map added with id {id}");
                    map.Id = id;

                    if (!uri.IsFile)
                    {
                        DownloadManager.AddDownloadToQueue(new Download(uri, map.LocalPath, (o, e) =>
                        {
                            using (var database = DatabaseManager.Open())
                            {
                                map.Status = "Valid";
                                database.Update(map);
                            }
                        }));
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

                    var map = db.Get(id);
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
                            map.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath));

                            DownloadManager.AddDownloadToQueue(new Download(uri, map.LocalPath, (o, e) =>
                            {
                                using (var database = DatabaseManager.Open())
                                {
                                    map.Status = "Valid";
                                    database.Update(map);
                                }
                            }));
                        }
                        map.Url = req.url;
                    }

                    int result = db.Update(map);
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
                    int result = db.Delete(id);
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
        }
    }
}
