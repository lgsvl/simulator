using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerRemoveParameters // (main.ContainerRemoveParameters)
    {
        [QueryStringParameter("v", false, typeof(BoolQueryStringConverter))]
        public bool? RemoveVolumes { get; set; }

        [QueryStringParameter("link", false, typeof(BoolQueryStringConverter))]
        public bool? RemoveLinks { get; set; }

        [QueryStringParameter("force", false, typeof(BoolQueryStringConverter))]
        public bool? Force { get; set; }
    }
}
