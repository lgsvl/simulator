using System.Net;

namespace Docker.DotNet
{
    public class DockerPluginNotFoundException : DockerApiException
    {
        public DockerPluginNotFoundException(HttpStatusCode statusCode, string responseBody) : base(statusCode, responseBody)
        {
        }
    }
}