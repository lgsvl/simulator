using System;
using System.IO;
using System.Linq;

using Database;
using FluentValidation;
using Nancy;
using Nancy.ModelBinding;

namespace Web.Modules
{
    public abstract class BaseModule<T> : NancyModule
    {
        protected string header = "";
        protected InlineValidator<T> addValidator = new InlineValidator<T>();
        protected InlineValidator<T> editValidator = new InlineValidator<T>();

        public BaseModule()
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
            Get($"/{header}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        return db.Query<T>().ToArray();
                    }
                }
                catch (Exception ex)
                {
                    return new
                    {
                        status = "error",
                        error = $"Failed to list {typeof(T).ToString()}: {ex.Message}.",
                    };
                }
            });
        }

        protected virtual void Status()
        {
            Get("/" + header + "/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int id = x.id;
                        return db.Single<T>(id);
                    }
                }
                catch (Exception ex)
                {
                    return new
                    {
                        status = "error",
                        error = $"Failed to get status for {typeof(T).ToString()}: {ex.Message}.",
                    };
                }
            });
        }

        protected virtual void Add()
        {
            Post($"/{header}", x =>
            {
                try
                {
                    object id;

                    using (var db = DatabaseManager.Open())
                    {
                        var boundObj = this.Bind<T>();
                        addValidator.ValidateAndThrow(boundObj);

                        id = db.Insert(boundObj);
                    }

                    // TODO: initiate download boundObj here if needed
                    // ...
                    return new
                    {
                        id = id,
                        status = "success"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        status = "error",
                        error = $"Failed to add {typeof(T).ToString()}: {ex.Message}.",
                    };
                }
            });
        }

        protected virtual void Update()
        {
            Put("/" + header + "/{id}", x =>
            {
                try
                {
                    var boundObj = this.Bind<T>();

                    using (var db = DatabaseManager.Open())
                    {
                        editValidator.ValidateAndThrow(boundObj);

                        // NOTE: condition is wrong, we should check for less then zero and more then one independently
                        if (db.Update(boundObj) != 1)
                        {
                            throw new Exception($"{header} does not exist");
                        }
                    }

                    return boundObj;
                }
                catch (Exception ex)
                {
                    return new
                    {
                        status = "error",
                        error = $"Failed to update {typeof(T).ToString()}: {ex.Message}."
                    };
                }
            });
        }

        protected virtual void Remove()
        {
            Delete("/" + header + "/{id}", x =>
            {
                try
                {
                    using (var db = DatabaseManager.Open())
                    {
                        int id = x.id;

                        // NOTE: condition is wrong, we should check for less then zero and more then one independently
                        if (db.Delete<T>(id) != 1)
                        {
                            throw new Exception("object does not exist");
                        }
                    }

                    return new { status = "success" };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        status = "error",
                        error = $"Failed to remove {typeof(T).ToString()}: {ex.Message}."
                    };
                }
            });
        }

        // TODO:
        // set status
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
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
