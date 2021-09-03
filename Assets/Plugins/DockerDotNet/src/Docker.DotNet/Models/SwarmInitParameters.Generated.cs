using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class SwarmInitParameters // (swarm.InitRequest)
    {
        [DataMember(Name = "ListenAddr", EmitDefaultValue = false)]
        public string ListenAddr { get; set; }

        [DataMember(Name = "AdvertiseAddr", EmitDefaultValue = false)]
        public string AdvertiseAddr { get; set; }

        [DataMember(Name = "DataPathAddr", EmitDefaultValue = false)]
        public string DataPathAddr { get; set; }

        [DataMember(Name = "DataPathPort", EmitDefaultValue = false)]
        public uint DataPathPort { get; set; }

        [DataMember(Name = "ForceNewCluster", EmitDefaultValue = false)]
        public bool ForceNewCluster { get; set; }

        [DataMember(Name = "Spec", EmitDefaultValue = false)]
        public Spec Spec { get; set; }

        [DataMember(Name = "AutoLockManagers", EmitDefaultValue = false)]
        public bool AutoLockManagers { get; set; }

        [DataMember(Name = "Availability", EmitDefaultValue = false)]
        public string Availability { get; set; }

        [DataMember(Name = "DefaultAddrPool", EmitDefaultValue = false)]
        public IList<string> DefaultAddrPool { get; set; }

        [DataMember(Name = "SubnetSize", EmitDefaultValue = false)]
        public uint SubnetSize { get; set; }
    }
}
