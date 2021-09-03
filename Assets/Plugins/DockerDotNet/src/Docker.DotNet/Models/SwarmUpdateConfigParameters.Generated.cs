using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmUpdateConfigParameters // (main.SwarmUpdateConfigParameters)
    {
        [DataMember(Name = "Config", EmitDefaultValue = false)]
        public SwarmConfigSpec Config { get; set; }

        [QueryStringParameter("version", true)]
        public long Version { get; set; }
    }
}
