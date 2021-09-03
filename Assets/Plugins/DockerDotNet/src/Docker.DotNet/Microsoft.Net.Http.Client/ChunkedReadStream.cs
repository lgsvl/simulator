using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Http.Client
{
    internal class ChunkedReadStream : Stream
    {
        private readonly BufferedReadStream _inner;
        private long _chunkBytesRemaining;
        private bool _disposed;
        private bool _done;

        public ChunkedReadStream(BufferedReadStream inner)
        {
            _inner = inner;
        }

        public override bool CanRead
        {
            get { return !_disposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return _inner.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int ReadTimeout
        {
            get
            {
                ThrowIfDisposed();
                return _inner.ReadTimeout;
            }
            set
            {
                ThrowIfDisposed();
                _inner.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                ThrowIfDisposed();
                return _inner.WriteTimeout;
            }
            set
            {
                ThrowIfDisposed();
                _inner.WriteTimeout = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // TODO: Validate buffer
            ThrowIfDisposed();

            if (_done)
            {
                return 0;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_chunkBytesRemaining == 0)
            {
                string headerLine = await _inner.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (!long.TryParse(headerLine, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _chunkBytesRemaining))
                {
                    throw new IOException("Invalid chunk header: " + headerLine);
                }
            }

            int read = 0;
            if (_chunkBytesRemaining > 0)
            {
                int toRead = (int)Math.Min(count, _chunkBytesRemaining);
                read = await _inner.ReadAsync(buffer, offset, toRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                _chunkBytesRemaining -= read;
            }

            if (_chunkBytesRemaining == 0)
            {
                // End of chunk, read the terminator CRLF
                var trailer = await _inner.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (trailer.Length > 0)
                {
                    throw new IOException("Invalid chunk trailer");
                }

                if (read == 0)
                {
                    _done = true;
                }
            }

            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: Sync drain with timeout if small number of bytes remaining?  This will let us re-use the connection.
                _inner.Dispose();
            }
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(ContentLengthReadStream).FullName);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }
    }
}
