using System;
using System.Net.Http;

namespace Docker.DotNet
{
    public abstract class Credentials : IDisposable
    {
        public abstract bool IsTlsCredentials();

        public abstract HttpMessageHandler GetHandler(HttpMessageHandler innerHandler);

        public virtual void Dispose()
        {
        }
    }
}