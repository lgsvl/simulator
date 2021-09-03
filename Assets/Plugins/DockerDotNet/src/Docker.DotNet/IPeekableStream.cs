namespace Docker.DotNet
{
    public interface IPeekableStream
    {
        /// <summary>
        /// Peek the underlying stream, can be used in order to avoid a blocking read call when no data is available
        /// https://stackoverflow.com/questions/6846365/check-for-eof-in-namedpipeclientstream
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa365779(v=vs.85).aspx
        /// </summary>
        /// <param name="buffer">buffer to put peeked data in</param>
        /// <param name="toPeek">max number of bytes to peek</param>
        /// <param name="peeked">number of bytes that were peeked</param>
        /// <param name="available">number of bytes that were available for peeking</param>
        /// <param name="remaining">number of available bytes minus number of peeked</param>
        /// <returns>whether peek operation succeeded</returns>
        bool Peek(byte[] buffer, uint toPeek, out uint peeked, out uint available, out uint remaining);
    }
}
