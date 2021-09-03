using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerProcessesResponse // (container.ContainerTopOKBody)
    {
        [DataMember(Name = "Processes", EmitDefaultValue = false)]
        public IList<IList<string>> Processes { get; set; }

        [DataMember(Name = "Titles", EmitDefaultValue = false)]
        public IList<string> Titles { get; set; }
    }
}
