using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageDeleteParameters // (main.ImageDeleteParameters)
    {
        [QueryStringParameter("force", false, typeof(BoolQueryStringConverter))]
        public bool? Force { get; set; }

        [QueryStringParameter("noprune", false, typeof(BoolQueryStringConverter))]
        public bool? NoPrune { get; set; }
    }
}
