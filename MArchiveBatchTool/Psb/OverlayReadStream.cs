using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MArchiveBatchTool.Psb
{
    /// <summary>
    /// Represents a stream with a section that has been filtered by a <see cref="IPsbFilter"/>.
    /// </summary>
    class OverlayReadStream : Stream
    {
        Stream baseStream;
        byte[] decryptedData;
        long overlayStart;
        long overlayEnd;
        bool isDisposed;

        /// <summary>
        /// Instantiates a new instance of <see cref="OverlayReadStream"/>.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="overlayStart">The starting offset of the filtered data.</param>
        /// <param name="overlayEnd">The ending offset of the filtered data.</param>
        /// <param name="filter">The filter to apply over the data.</param>
        /// <exception cref="IOException">If the data to be filtered cannot be read.</exception>
        public OverlayReadStream(Stream baseStream, uint overlayStart, uint overlayEnd, IPsbFilter filter)
        {
            this.baseStream = baseStream;
            this.overlayStart = overlayStart;
            this.overlayEnd = overlayEnd;
            long origBasePos = baseStream.Position;
            baseStream.Seek(overlayStart, SeekOrigin.Begin);
            decryptedData = new byte[(int)(overlayEnd - overlayStart)];
            if (baseStream.Read(decryptedData, 0, decryptedData.Length) != decryptedData.Length)
                throw new IOException("Could not read all bytes in overlay region.");
            filter.Filter(decryptedData);
            baseStream.Position = origBasePos;
        }

        /// <inheritdoc/>
        public override bool CanRead => baseStream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => baseStream.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => baseStream.Length;

        /// <inheritdoc/>
        public override long Position { get => baseStream.Position; set => baseStream.Position = value; }

        /// <inheritdoc/>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset is negative.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count is negative.");
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset), "Offset is past the end of buffer.");
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "Offset and count exceeds end of buffer.");

            if (isDisposed) throw new ObjectDisposedException(GetType().FullName);

            int totalRead = 0;
            while (count > 0)
            {
                if (Position < overlayStart)
                {
                    int bytesInOverlay = (int)(Position + count - overlayStart);
                    if (bytesInOverlay < 0) bytesInOverlay = 0;
                    int bytesToRead = count - bytesInOverlay;
                    int read = baseStream.Read(buffer, offset, bytesToRead);
                    totalRead += read;
                    offset += read;
                    count -= read;
                    if (read != bytesToRead) break;
                }
                else if (Position >= overlayEnd)
                {
                    totalRead += baseStream.Read(buffer, offset, count);
                    break;
                }
                else
                {
                    int bytesOutsideOverlay = (int)(Position + count - overlayEnd);
                    if (bytesOutsideOverlay < 0) bytesOutsideOverlay = 0;
                    int bytesToRead = count - bytesOutsideOverlay;
                    Buffer.BlockCopy(decryptedData, (int)(Position - overlayStart), buffer, offset, bytesToRead);
                    totalRead += bytesToRead;
                    offset += bytesToRead;
                    count -= bytesToRead;
                    baseStream.Position += bytesToRead;
                }
            }
            return totalRead;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return baseStream.Seek(offset, origin);
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                isDisposed = true;
                baseStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
