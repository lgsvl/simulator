using System.Net;

namespace Docker.DotNet
{
    internal class DockerApiResponse
    {
        public HttpStatusCode StatusCode { get; private set; }

        public string Body { get; private set; }

        public DockerApiResponse(HttpStatusCode statusCode, string body)
        {
            this.StatusCode = statusCode;
            this.Body = body;
        }
    }
}