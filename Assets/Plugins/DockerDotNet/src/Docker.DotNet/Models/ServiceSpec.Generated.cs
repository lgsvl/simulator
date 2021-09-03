using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ServiceSpec // (swarm.ServiceSpec)
    {
        public ServiceSpec()
        {
        }

        public ServiceSpec(Annotations Annotations)
        {
            if (Annotations != null)
            {
                this.Name = Annotations.Name;
                this.Labels = Annotations.Labels;
            }
        }

        [DataMember(Name = "Name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "Labels", EmitDefaultValue = false)]
        public IDictionary<string, string> Labels { get; set; }

        [DataMember(Name = "TaskTemplate", EmitDefaultValue = false)]
        public TaskSpec TaskTemplate { get; set; }

        [DataMember(Name = "Mode", EmitDefaultValue = false)]
        public ServiceMode Mode { get; set; }

        [DataMember(Name = "UpdateConfig", EmitDefaultValue = false)]
        public SwarmUpdateConfig UpdateConfig { get; set; }

        [DataMember(Name = "RollbackConfig", EmitDefaultValue = false)]
        public SwarmUpdateConfig RollbackConfig { get; set; }

        [DataMember(Name = "Networks", EmitDefaultValue = false)]
        public IList<NetworkAttachmentConfig> Networks { get; set; }

        [DataMember(Name = "EndpointSpec", EmitDefaultValue = false)]
        public EndpointSpec EndpointSpec { get; set; }
    }
}
