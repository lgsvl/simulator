using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class NodeRemoveParameters
    {
        [QueryStringParameter("force", false)]
        public bool Force { get; set; }
    }
}