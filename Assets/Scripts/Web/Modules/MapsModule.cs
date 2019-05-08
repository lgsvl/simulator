using Database;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using UnityEngine;

namespace Web.Modules
{
    public class MapRequest
    {
        public string name;
        public string url;
    }

    public class MapResponse : WebResponse
    {
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string LocalPath;
        public string Status;
    }

    public class MapsModule : BaseModule<Map, MapRequest, MapResponse>
    {
        public MapsModule() : base("maps")
        {
            addValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            addValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            addValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
            addValidator.RuleFor(o => o.Url).Must(BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle File");
            editValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            editValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            editValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
            editValidator.RuleFor(o => o.Url).Must(BeValidAssetBundle).WithMessage("You must specify a valid AssetBundle File");
            Preview();
            base.Init();
        }
        
        protected override void Add()
        {
            Post($"/", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = this.Bind<MapRequest>();
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

                        if (!uri.IsFile) { 
                            DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", Path.GetFileName(uri.AbsolutePath)), (o, e) =>
                            {
                                using (var database = DatabaseManager.Open())
                                {
                                    Map updatedModel = db.Single<Map>(id);
                                    updatedModel.Status = "Valid";
                                    db.Update(updatedModel);
                                }
                            }));
                        }

                        Debug.Log($"Adding {typeof(Map).ToString()} with id {model.Id}");

                        return ConvertToResponse(model);
                    }
                }
                catch (ValidationException ex)
                {
                    Debug.Log($"Failed to add {typeof(Map).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to add {typeof(Map).ToString()}: {ex.Message}."
                    }, HttpStatusCode.BadRequest);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to add {typeof(Map).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to add {typeof(Map).ToString()}: {ex.Message}."
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
                        var boundObj = this.Bind<MapRequest>();
                        Map model = ConvertToModel(boundObj);
                        model.Id = x.id;

                        editValidator.ValidateAndThrow(model);
                        int id = x.id;
                        Map originalModel = db.Single<Map>(id);

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
                                model.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath));
                                DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath)), (o, e) =>
                                {
                                    using (var database = DatabaseManager.Open())
                                    {
                                        Map updatedModel = db.Single<Map>(id);
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

                        Debug.Log($"Updating {typeof(Map).ToString()} with id {model.Id}");
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

        protected override Map ConvertToModel(MapRequest mapRequest)
        {
            Map map = new Map();
            map.Name = mapRequest.name;
            map.Url = mapRequest.url;
            map.Status = "Valid";
            return map;
        }

        public override MapResponse ConvertToResponse(Map map)
        {
            MapResponse mapResponse = new MapResponse();
            mapResponse.Name = map.Name;
            mapResponse.Url = map.Url;
            mapResponse.LocalPath = map.LocalPath;
            mapResponse.Status = map.Status;
            mapResponse.Id = map.Id;
            return mapResponse;
        }

        // NOTE: Let's rename to BeValidAssetBundle and
        //       check that file content starts with 'UnityFS'
        // Open bundle read first 7 bytes
        protected bool BeValidAssetBundle(string url)
        {
            Uri uri = new Uri(url);
            byte[] buffer = new byte[7];
            try
            {
                if (uri.IsFile)
                {
                    using (FileStream fs = new FileStream(uri.AbsolutePath, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(buffer, 0, buffer.Length);
                        fs.Close();
                        Debug.Log(Encoding.UTF8.GetString(buffer));
                        return Encoding.UTF8.GetString(buffer) == "UnityFS";
                    }
                }
                else
                {
                    //Todo: check remote file
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        protected void Preview()
        {
            Get("/{id}/preview", x => HttpStatusCode.NotFound);
        }
    }
}