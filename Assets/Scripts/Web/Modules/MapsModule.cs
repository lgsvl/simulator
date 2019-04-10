using System;
using System.IO;

using Database;
using Database.Models;
using FluentValidation;

namespace Web.Modules
{
    public class MapsModule : BaseModule<Map>
    {
        public MapsModule()
        {
            header = "maps";
            addValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            addValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            addValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");

            editValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            editValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            editValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
            Preview();
            base.Init();
        }

        protected void Preview()
        {
            Get("/maps/{id}/preview", x => {
                int id = x.id;
                Uri uri = new Uri(DatabaseManager.db.Single<Map>(id).PreviewUrl);
                try
                {
                    if (File.Exists(uri.LocalPath))
                    {
                        return File.ReadAllBytes(uri.LocalPath);
                    }
                }
                catch(Exception ex)
                {
                    return ex;
                }

                return null;
            });
        }
    }
}