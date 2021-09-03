using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Http.Client
{
    internal class ChunkedWriteStream : Stream
    {
        private static readonly byte[] s_EndContentBytes = Encoding.ASCII.GetBytes("0\r\n\r\n");

        private Stream _innerStream;

        public ChunkedWriteStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _innerStream = stream;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count == 0)
            {
                return;
            }

            var chunkSize = Encoding.ASCII.GetBytes(count.ToString("x") + "\r\n");
            await _innerStream.WriteAsync(chunkSize, 0, chunkSize.Length, cancellationToken).ConfigureAwait(false);
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            await _innerStream.WriteAsync(chunkSize, chunkSize.Length - 2, 2, cancellationToken).ConfigureAwait(false);
        }

        public Task EndContentAsync(CancellationToken cancellationToken)
        {
            return _innerStream.WriteAsync(s_EndContentBytes, 0, s_EndContentBytes.Length, cancellationToken);
        }
    }
}