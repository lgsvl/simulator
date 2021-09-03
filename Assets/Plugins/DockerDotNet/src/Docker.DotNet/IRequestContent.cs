using System.Net.Http;

namespace Docker.DotNet
{
    internal interface IRequestContent
    {
        HttpContent GetContent();
    }
}