using System.IO;

namespace Microsoft.Net.Http.Client
{
    public abstract class WriteClosableStream : Stream
    {
        public abstract bool CanCloseWrite { get; }

        public abstract void CloseWrite();
    }
}