using Database;
using FluentValidation;
using Nancy;

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

            editValidator.RuleFor(o => o.Url).NotNull().NotEmpty().WithMessage("You must specify a non-empty, unique URL");
            editValidator.RuleFor(o => o.Url).Must(BeValidFilePath).WithMessage("You must specify a valid URL");
            editValidator.RuleFor(o => o.Name).NotEmpty().WithMessage("You must specify a non-empty name");
            Preview();
            base.Init();
        }
        
        protected override Map ConvertToModel(MapRequest mapRequest)
        {
            Map map = new Map();
            map.Name = mapRequest.name;
            map.Url = mapRequest.url;
            map.Status = "1";
            return map;
        }

        protected override MapResponse ConvertToResponse(Map map)
        {
            MapResponse mapResponse = new MapResponse();
            mapResponse.Name = map.Name;
            mapResponse.Url = map.Url;
            mapResponse.Status = map.Status;
            mapResponse.Id = map.Id;
            return mapResponse;
        }

        protected void Preview()
        {
            Get("/maps/{id}/preview", x => HttpStatusCode.NotFound);
        }
    }
}