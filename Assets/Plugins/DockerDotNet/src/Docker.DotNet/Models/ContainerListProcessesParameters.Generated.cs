using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerListProcessesParameters // (main.ContainerListProcessesParameters)
    {
        [QueryStringParameter("ps_args", false)]
        public string PsArgs { get; set; }
    }
}
