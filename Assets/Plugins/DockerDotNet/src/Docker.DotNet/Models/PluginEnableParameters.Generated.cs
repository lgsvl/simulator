using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class PluginEnableParameters // (main.PluginEnableParameters)
    {
        [QueryStringParameter("timeout", false)]
        public long? Timeout { get; set; }
    }
}
