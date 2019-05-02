using Database;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.IO; 
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
        public string Status;
    }

    public class MapsModule : BaseModule<Map, MapRequest, MapResponse>
    {
        public MapsModule()
        {
            header = "maps";
            addValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            addValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            addValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
            // NOTE: This is wrong we are not validating Mime-Type of the URL (there is no such thing).
            //       For local bundles we check that file content starts with 'UnityFS'.
            //       For remote bundles we check that response from hosting server is 'application/octet-stream'.
            //addValidator.RuleFor(o => o.Url).Must(BeValidMimeType).WithMessage("You must specify a valid Mime Type");
            editValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            editValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            editValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
            // NOTE: This is wrong we are not validating Mime-Type of the URL (there is no such thing).
            //       For local bundles we check that file content starts with 'UnityFS'.
            //       For remote bundles we check that response from hosting server is 'application/octet-stream'.
            //editValidator.RuleFor(o => o.Url).Must(BeValidMimeType).WithMessage("You must specify a valid Mime Type");
            Preview();
            base.Init();
        }

        //protected override void Add()
        //{
        //    Post($"/{header}", x =>
        //    {
        //        try
        //        {
        //            using (var db = DatabaseManager.Open())
        //            {
        //                var boundObj = this.Bind<MapRequest>();
        //                var model = ConvertToModel(boundObj);
        //                addValidator.Validate(model);

        //                Uri uri = new Uri(model.Url);
        //                if (uri.IsFile)
        //                {
        //                    model.LocalPath = model.Url;
        //                }
        //                else
        //                {
        //                    model.LocalPath = Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath));
        //                    DownloadManager.AddDownloadToQueue(new Download(uri, Path.Combine(DownloadManager.dataPath, "..", "AssetBundles/Environments", Path.GetFileName(uri.AbsolutePath))));
        //                }

        //                object id = db.Insert(model);
        //                Debug.Log($"Adding {typeof(Map).ToString()} with id {model.Id}");

        //                return new
        //                {
        //                    responseStatus = "success",
        //                    model = ConvertToResponse(model),
        //                };
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.Log($"Failed to add {typeof(Map).ToString()}: {ex.Message}.");
        //            return new
        //            {
        //                responseStatus = "error",
        //                error = $"Failed to add {typeof(Map).ToString()}: {ex.Message}."
        //            };
        //        }
        //    });
        //}

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
            mapResponse.Status = map.Status;
            mapResponse.Id = map.Id;
            return mapResponse;
        }

        // NOTE: Let's rename to BeValidAssetBundle and
        //       check that file content starts with 'UnityFS'
        protected bool BeValidMimeType(string url)
        {
            Uri uri = new Uri(url);
            string type = MimeTypes.GetMimeType(Path.GetFileName(uri.AbsolutePath));
            Debug.Log(type);
            return type == "application/octet-stream";
        }

        protected void Preview()
        {
            Get("/maps/{id}/preview", x => HttpStatusCode.NotFound);
        }
    }
}