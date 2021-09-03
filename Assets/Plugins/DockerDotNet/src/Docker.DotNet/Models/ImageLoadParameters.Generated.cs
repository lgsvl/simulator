using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ImageLoadParameters // (main.ImageLoadParameters)
    {
        [QueryStringParameter("quiet", true, typeof(BoolQueryStringConverter))]
        public bool Quiet { get; set; }
    }
}
