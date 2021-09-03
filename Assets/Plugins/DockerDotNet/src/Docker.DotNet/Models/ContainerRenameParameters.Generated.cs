using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    [DataContract]
    public class ContainerRenameParameters // (main.ContainerRenameParameters)
    {
        [QueryStringParameter("name", false)]
        public string NewName { get; set; }
    }
}
