using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class EngineDescription // (swarm.EngineDescription)
    {
        [DataMember(Name = "EngineVersion", EmitDefaultValue = false)]
        public string EngineVersion { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "Plugins", EmitDefaultValue = false)]
        public IList<PluginDescription> Plugins { get; set; }
    }
}
