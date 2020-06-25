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
    /// Represents a stream that can compress to a FastLZ compressed block.
    /// </summary>
    public class FastLzCompressionStream : Stream
    {
        Stream baseStream;
        MemoryStream buffer = new MemoryStream();
        bool finalized;
        int level;
        bool leaveOpen;

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => !finalized;

        /// <inheritdoc/>
        public override long Length => buffer.Length;

        /// <inheritdoc/>
        public override long Position { get => buffer.Length; set => throw new NotSupportedException(); }

        /// <summary>
        /// Instantiates a new instance of <see cref="FastLzCompressionStream"/>.
        /// </summary>
        /// <param name="baseStream">The stream to write compressed data to.</param>
        /// <param name="level">The compression level. Can be 1 or 2.</param>
        /// <param name="leaveOpen">Leave <paramref name="baseStream"/> open when this stream is closed.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is unknown.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="baseStream"/> is <c>null</c>.</exception>
        public FastLzCompressionStream(Stream baseStream, int level, bool leaveOpen = false)
        {
            if (level < 1 || level > 2) throw new ArgumentOutOfRangeException(nameof(level), "Unknown compression level.");
            this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            this.level = level;
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Performs the compression operation and writes the compressed block to the underlying stream.
        /// </summary>
        /// <remarks>
        /// Call only when all data to be compressed has been written to the stream. Subsequent calls
        /// will not do anything.
        /// </remarks>
        public override void Flush()
        {
            if (finalized) return;
            buffer.Flush();
            byte[] output = new byte[Math.Max(66, (int)Math.Ceiling(buffer.Length * 1.05))];
            int compressedLength = FastLz.Compress(level, buffer.ToArray(), output);

            //// Debug
            //Array.Resize(ref output, compressedLength);
            //File.WriteAllBytes("compressed.bin", output);

            baseStream.Write(output, 0, compressedLength);
            baseStream.Flush();
            finalized = true;
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Write data to be compressed.
        /// </summary>
        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (finalized) throw new InvalidOperationException("Data compression already finalized.");
            this.buffer.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!finalized)
                {
                    Flush();
                }
                if (!leaveOpen)
                {
                    baseStream.Close();
                }
            }
            base.Dispose(disposing);
        }
    }
}
