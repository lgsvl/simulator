using Database;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using System;
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
                            model.LocalPath = uri.LocalPath;
                        }
                        else
                        {
                            model.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath));
                            DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath))));
                        }

                        object id = db.Insert(model);
                        Debug.Log($"Adding {typeof(Map).ToString()} with id {model.Id}");

                        return ConvertToResponse(model);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to add {typeof(Map).ToString()}: {ex.Message}.");
                    return new
                    {
                        responseStatus = "error",
                        error = $"Failed to add {typeof(Map).ToString()}: {ex.Message}."
                    };
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

                        if (model.LocalPath != originalModel.LocalPath)
                        {
                            Uri uri = new Uri(model.Url);
                            if (uri.IsFile)
                            {
                                model.LocalPath = model.Url;
                            }
                            else
                            {
                                model.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath));
                                DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath))));
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

                        model.Status = "Valid";

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