using System.Net;

namespace Docker.DotNet
{
    public class DockerContainerNotFoundException : DockerApiException
    {
        public DockerContainerNotFoundException(HttpStatusCode statusCode, string responseBody) : base(statusCode, responseBody)
        {
        }
    }
}