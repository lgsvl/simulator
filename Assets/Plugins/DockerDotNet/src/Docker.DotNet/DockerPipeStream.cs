using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Http.Client;

namespace Docker.DotNet
{
    internal class DockerPipeStream : WriteClosableStream, IPeekableStream
    {
        private readonly PipeStream _stream;
        private readonly EventWaitHandle _event = new EventWaitHandle(false, EventResetMode.AutoReset);

        public DockerPipeStream(PipeStream stream)
        {
            _stream = stream;
        }

        public override bool CanCloseWrite => true;

        public override bool CanRead => true;

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

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        private static extern int WriteFile(SafeHandle handle, IntPtr buffer, int numBytesToWrite, IntPtr numBytesWritten, ref NativeOverlapped overlapped);

        [DllImport("api-ms-win-core-io-l1-1-0.dll", SetLastError = true)]
        private static extern int GetOverlappedResult(SafeHandle handle, ref NativeOverlapped overlapped, out int numBytesWritten, int wait);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PeekNamedPipe(SafeHandle handle, byte[] buffer, uint nBufferSize, ref uint bytesRead, ref uint bytesAvail, ref uint BytesLeftThisMessage);

        public override void Close()
        {
            _stream.Close();
            base.Close();
        }

        public override void CloseWrite()
        {
            // The Docker daemon expects a write of zero bytes to signal the end of writes. Use native
            // calls to achieve this since CoreCLR ignores a zero-byte write.
            var overlapped = new NativeOverlapped();

#if NET45
            var handle = _event.SafeWaitHandle;
#else
            var handle = _event.GetSafeWaitHandle();
#endif

            // Set the low bit to tell Windows not to send the result of this IO to the
            // completion port.
            overlapped.EventHandle = (IntPtr)(handle.DangerousGetHandle().ToInt64() | 1);
            if (WriteFile(_stream.SafePipeHandle, IntPtr.Zero, 0, IntPtr.Zero, ref overlapped) == 0)
            {
                const int ERROR_IO_PENDING = 997;
                if (Marshal.GetLastWin32Error() == ERROR_IO_PENDING)
                {
                    int written;
                    if (GetOverlappedResult(_stream.SafePipeHandle, ref overlapped, out written, 1) == 0)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }
                else
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public bool Peek(byte[] buffer, uint toPeek, out uint peeked, out uint available, out uint remaining)
        {
            peeked = 0;
            available = 0;
            remaining = 0;

            bool aPeekedSuccess = PeekNamedPipe(
                _stream.SafePipeHandle,
                buffer, toPeek,
                ref peeked, ref available, ref remaining);

            var error = Marshal.GetLastWin32Error();

            if (error == 0 && aPeekedSuccess)
            {
                return true;
            }

            return false;
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
            _stream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
                _event.Dispose();
            }
        }
    }
}
