using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerEventsParameters // (main.ContainerEventsParameters)
    {
        [QueryStringParameter("since", false)]
        public string Since { get; set; }

        [QueryStringParameter("until", false)]
        public string Until { get; set; }

        [QueryStringParameter("filters", false, typeof(MapQueryStringConverter))]
        public IDictionary<string, IDictionary<string, bool>> Filters { get; set; }
    }
}
