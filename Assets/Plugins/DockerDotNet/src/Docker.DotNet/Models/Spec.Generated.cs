using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class Spec // (swarm.Spec)
    {
        public Spec()
        {
        }

        public Spec(Annotations Annotations)
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

        [DataMember(Name = "Orchestration", EmitDefaultValue = false)]
        public OrchestrationConfig Orchestration { get; set; }

        [DataMember(Name = "Raft", EmitDefaultValue = false)]
        public RaftConfig Raft { get; set; }

        [DataMember(Name = "Dispatcher", EmitDefaultValue = false)]
        public DispatcherConfig Dispatcher { get; set; }

        [DataMember(Name = "CAConfig", EmitDefaultValue = false)]
        public CAConfig CAConfig { get; set; }

        [DataMember(Name = "TaskDefaults", EmitDefaultValue = false)]
        public TaskDefaults TaskDefaults { get; set; }

        [DataMember(Name = "EncryptionConfig", EmitDefaultValue = false)]
        public EncryptionConfig EncryptionConfig { get; set; }
    }
}
