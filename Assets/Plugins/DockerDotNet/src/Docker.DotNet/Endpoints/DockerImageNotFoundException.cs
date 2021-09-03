using System.Net;

namespace Docker.DotNet
{
    public class DockerImageNotFoundException : DockerApiException
    {
        public DockerImageNotFoundException(HttpStatusCode statusCode, string body) : base(statusCode, body)
        {
        }
    }
}