using System.IO;

namespace Docker.DotNet.Models
{
    public class GetArchiveFromContainerResponse
    {
        public ContainerPathStatResponse Stat { get; set; }

        public Stream Stream { get; set; }
    }
}
