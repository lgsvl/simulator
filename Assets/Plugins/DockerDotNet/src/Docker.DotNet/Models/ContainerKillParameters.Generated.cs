using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerKillParameters // (main.ContainerKillParameters)
    {
        [QueryStringParameter("signal", false)]
        public string Signal { get; set; }
    }
}
