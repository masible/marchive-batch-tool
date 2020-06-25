// SPDX-License-Identifier: MIT
/*
  FastLZ - Byte-aligned LZ77 compression library
  Copyright (C) 2020 Yukai Li

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FastLz
{
    /// <summary>
    /// Represents a stream that can decompress FastLZ compressed block.
    /// </summary>
    public class FastLzDecompressionStream : Stream
    {
        bool disposed;
        byte[] buffer;
        long bufferPos;

        /// <inheritdoc/>
        public override bool CanRead => !disposed;

        /// <inheritdoc/>
        public override bool CanSeek => !disposed;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => buffer.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                if (disposed) throw new ObjectDisposedException(GetType().FullName);
                return bufferPos;
            }
            set
            {
                if (disposed) throw new ObjectDisposedException(GetType().FullName);
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be negative.");
                bufferPos = value;
            }
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="FastLzDecompressionStream"/>.
        /// </summary>
        /// <param name="baseStream">The base stream to read compressed data from.</param>
        /// <param name="decompressedLength">The length of the decompressed data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="baseStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="baseStream"/> is not seekable.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="decompressedLength"/> is negative.</exception>
        public FastLzDecompressionStream(Stream baseStream, int decompressedLength)
        {
            if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanSeek) throw new ArgumentException("Base stream is not seekable.", nameof(baseStream));
            if (decompressedLength < 0) throw new ArgumentOutOfRangeException(nameof(decompressedLength), "Length cannot be negative.");

            buffer = new byte[decompressedLength];
            byte[] compressed = new BinaryReader(baseStream).ReadBytes((int)(baseStream.Length - baseStream.Position));

            //// Debug
            //File.WriteAllBytes("src.bin", compressed);

            decompressedLength = FastLz.Decompress(compressed, buffer);
            Array.Resize(ref buffer, decompressedLength);
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            if (disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (disposed) throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            if (offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset), "Offset is past the end of buffer.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset), "Trying to write past end of buffer.");

            if (bufferPos >= this.buffer.Length) return 0;
            int read = (int)Math.Min(count, this.buffer.Length - bufferPos);
            Buffer.BlockCopy(this.buffer, (int)bufferPos, buffer, offset, read);
            bufferPos += read;
            return read;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (disposed) throw new ObjectDisposedException(GetType().FullName);
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
                    bufferPos = offset;
                    break;
                case SeekOrigin.Current:
                    if (bufferPos + offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Attempting to set to a negative position.");
                    bufferPos += offset;
                    break;
                case SeekOrigin.End:
                    if (buffer.Length + offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Attempting to set to a negative position.");
                    bufferPos = buffer.Length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(offset), "Unknown seek origin.");
            }
            return bufferPos;
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
                disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
