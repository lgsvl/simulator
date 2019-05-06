﻿using Database;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Web.Modules
{
    public abstract class BaseModule<Model, ModuleRequest, ModuleResponse> : NancyModule
        where Model : DatabaseModel
        where ModuleResponse : WebResponse
    {
        protected InlineValidator<Model> addValidator = new InlineValidator<Model>();
        protected InlineValidator<Model> editValidator = new InlineValidator<Model>();


        protected abstract Model ConvertToModel(ModuleRequest request);
        public abstract ModuleResponse ConvertToResponse(Model model);

        public BaseModule(string basePath) : base(basePath)
        {
        }

        protected virtual void Init()
        {
            List();
            Status();
            Add();
            Update();
            Remove();
        }

        protected virtual void List()
        {
            Get("/", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int page = this.Request.Query["page"];

                        // 5 is just an arbitrary value to ensure that we don't try and Page a count of 0
                        int count = this.Request.Query["count"] > 0 ? this.Request.Query["count"] : 5;
                        var models = db.Page<Model>(page, count).Items;
                        Debug.Log($"Listing {ModulePath}");
                        return models.Select(m => ConvertToResponse(m)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to list {typeof(Model).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to list {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected virtual void Status()
        {
            Get("/{id}", x =>
            {
                try
                {
                    int id = x.id;
                    using (var db = DatabaseManager.Open())
                    {
                        ModuleResponse response = ConvertToResponse(db.Single<Model>(id));
                        response.Id = id;
                        Debug.Log($"Getting {typeof(Model).ToString()} with id {id}");
                        return response;
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to find {typeof(Model).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to find {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to get status for {typeof(Model).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to get status for {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected virtual void Add()
        {
            Post("/", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = this.Bind<ModuleRequest>();
                        var model = ConvertToModel(boundObj);
                        addValidator.ValidateAndThrow(model);
                        model.Status = "Valid";
                        object id = db.Insert(model);
                        Debug.Log($"Adding {typeof(Model).ToString()} with id {model.Id}");
                        return ConvertToResponse(model);
                    }
                }
                catch (ValidationException ex)
                {
                    Debug.Log($"Failed to add {typeof(Model).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to add {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.BadRequest);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to add {typeof(Model).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to add {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected virtual void Update()
        {
            Put("/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = this.Bind<ModuleRequest>();
                        Model model = ConvertToModel(boundObj);
                        model.Id = x.id;
                        editValidator.ValidateAndThrow(model);

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

                        Debug.Log($"Updating {typeof(Model).ToString()} with id {model.Id}");
                        return ConvertToResponse(model);
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to update {typeof(Model).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to update {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to update {typeof(Model).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to update {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        protected virtual void Remove()
        {
            Delete("/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int id = x.id;
                        int result = db.Delete<Model>(id);
                        if (result > 1)
                        {
                            throw new Exception($"more than one object has id {id}");
                        }

                        if (result < 1)
                        {
                            throw new IndexOutOfRangeException($"id {x.id} does not exist");
                        }

                        Debug.Log($"Removing {typeof(Model).ToString()} with id {id}");
                    }

                    return HttpStatusCode.OK;
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.Log($"Failed to remove {typeof(Model).ToString()}: {ex.Message}.");
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to remove {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.NotFound);
                    return r;
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to remove {typeof(Model).ToString()}");
                    Debug.LogException(ex);
                    Response r = Response.AsJson(new
                    {
                        error = $"Failed to remove {typeof(Model).ToString()}: {ex.Message}."
                    }, HttpStatusCode.InternalServerError);
                    return r;
                }
            });
        }

        // TODO:
        // set responseStatus
        // if url is new:
        // if url starts with file:// and file exists set localPath, if it does not exist throw an exception
        // if url starts with http:// or https:// create a temporary file and initiate downloading
        //    when downloading is completed move file to expected location and update localPath
        // otherwise throw exception with error message
        protected static bool BeValidFilePath(string url)
        {
            Uri uri = new Uri(url);
            if (uri.IsFile)
            {
                if (!File.Exists(uri.LocalPath))
                {
                    Debug.Log($"BeValidFilePath validation failed for {url}: there is no file at the given path");
                    return false;
                }
            }
            else
            {
                return uri.IsWellFormedOriginalString();
            }

            return true;
        }
    }
}