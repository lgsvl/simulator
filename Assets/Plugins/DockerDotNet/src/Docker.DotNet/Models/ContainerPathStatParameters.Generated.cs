using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerPathStatParameters // (main.ContainerPathStatParameters)
    {
        [QueryStringParameter("path", true)]
        public string Path { get; set; }

        [QueryStringParameter("noOverwriteDirNonDir", false, typeof(BoolQueryStringConverter))]
        public bool? AllowOverwriteDirWithFile { get; set; }
    }
}
