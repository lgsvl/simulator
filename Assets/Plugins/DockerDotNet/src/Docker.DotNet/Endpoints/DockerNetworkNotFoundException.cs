using System.Net;

namespace Docker.DotNet
{
    public class DockerNetworkNotFoundException : DockerApiException
    {
        public DockerNetworkNotFoundException(HttpStatusCode statusCode, string responseBody) : base(statusCode, responseBody)
        {
        }
    }
}