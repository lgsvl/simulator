using Database.Models;
using FluentValidation;
using Nancy;

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
            Get("/maps/{id}/preview", x => HttpStatusCode.NotFound);
        }
    }
}