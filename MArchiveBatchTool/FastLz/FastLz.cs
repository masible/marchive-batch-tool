// SPDX-License-Identifier: MIT
/*
  FastLZ - Byte-aligned LZ77 compression library
  Copyright (C) 2020 Yukai Li
  Copyright (C) 2005-2020 Ariya Hidayat <ariya.hidayat@gmail.com>

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
using System.Diagnostics;
using System.Text;

namespace FastLz
{
    /// <summary>
    /// Class for compressing and decompressing with FastLZ.
    /// </summary>
    public static class FastLz
    {
        /// <summary>
        /// Gets the version of the library.
        /// </summary>
        public static Version Version { get; } = new Version(0, 5, 0);

        /// <summary>
        /// Compress a block of data in the input buffer and returns the size of
        /// compressed block.
        /// 
        /// If the input is not compressible, the return value might be larger than
        /// the input buffer size.
        /// 
        /// The input buffer and the output buffer can not overlap.
        /// 
        /// Compression level can be specified in <paramref name="level"/>. At the moment,
        /// only level 1 and level 2 are supported.
        /// Level 1 is the fastest compression and generally useful for short data.
        /// Level 2 is slightly slower but it gives better compression ratio.
        /// 
        /// Note that the compressed data, regardless of the level, can always be
        /// decompressed using <see cref="Decompress(byte[], byte[])"/>.
        /// </summary>
        /// <param name="level">The compression level.</param>
        /// <param name="input">The buffer with the data to compress. The minimum input buffer size is 16.</param>
        /// <param name="output">The buffer to compress into. The output buffer must be at least 5% larger than the input buffer and can not be smaller than 66 bytes.</param>
        /// <returns>The size of the compressed block.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="input"/> or <paramref name="output"/> is too small.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is unknown.</exception>
        public static int Compress(int level, byte[] input, byte[] output)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length < 16) throw new ArgumentException("Input buffer is less than 16 bytes in length.", nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (output.Length < 66 || output.Length < (int)Math.Ceiling(input.Length * 1.05))
                throw new ArgumentException("Output buffer is too small.", nameof(output));

            switch (level)
            {
                case 1:
                    return Level1Compress(input, output);
                case 2:
                    return Level2Compress(input, output);
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), "Unknown compression level.");
            }
        }

        /// <summary>
        /// Decompress a block of compressed data and returns the size of the
        /// decompressed block. If error occurs, e.g. the compressed data is
        /// corrupted or the output buffer is not large enough, then an
        /// exception will be thrown instead.
        /// 
        /// The input buffer and the output buffer can not overlap.
        /// 
        /// Note that the decompression will always work, regardless of the
        /// compression level specified in <see cref="Compress(int, byte[], byte[])"/>
        /// (when producing the compressed block).
        /// </summary>
        /// <param name="input">The buffer containing compressed data.</param>
        /// <param name="output">The buffer to decompress data into.</param>
        /// <returns>The size of the decompressed data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The compressed block in <paramref name="input"/> is using an unknown level.</exception>
        public static int Decompress(byte[] input, byte[] output)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));

            int level = (input[0] >> 5) + 1;
            switch (level)
            {
                case 1:
                    return Level1Decompress(input, output);
                case 2:
                    return Level2Decompress(input, output);
                default:
                    throw new ArgumentException("Unknown compression level.", nameof(input));
            }
        }

        #region Implementation

        static void BoundCheck(bool condition)
        {
            // Not sure what to put here
            // Runtime exceptions should take care of things
        }

        static void MemMove(byte[] dest, uint destOffset, byte[] src, uint srcOffset, uint count)
        {
            for (uint i = 0; i < count; ++i)
            {
                dest[destOffset + i] = src[srcOffset + i];
            }
        }

        static void MemCpy(byte[] dest, uint destOffset, byte[] src, uint srcOffset, uint count)
        {
            Buffer.BlockCopy(src, (int)srcOffset, dest, (int)destOffset, (int)count);
        }

        static uint ReadU32(byte[] buffer, uint offset)
        {
            return BitConverter.ToUInt32(buffer, (int)offset);
        }

        static uint Cmp(byte[] x, uint xOffset, byte[] y, uint yOffset, uint length)
        {
            for (uint i = 0; i < length; ++i)
            {
                if (x[xOffset + i] != y[yOffset + i]) return i + 1;
            }

            return length;
        }

        // Copy 64-bit blocks
        static void Copy64(byte[] dest, uint destOffset, byte[] src, uint srcOffset, uint count)
        {
            MemCpy(dest, destOffset, src, srcOffset, count * 8);
        }

        // Copy 256 bits
        static void Copy256(byte[] dest, uint destOffset, byte[] src, uint srcOffset)
        {
            MemCpy(dest, destOffset, src, srcOffset, 32);
        }

        const int MAX_COPY = 32;
        const int MAX_LEN = 256 + 8;
        const int MAX_L1_DISTANCE = 8192;
        const int MAX_L2_DISTANCE = 8191;
        const int MAX_FARDISTANCE = 65535 + MAX_L2_DISTANCE - 1;

        const int HASH_LOG = 14;
        const int HASH_SIZE = 1 << HASH_LOG;
        const int HASH_MASK = HASH_SIZE - 1;

        static ushort Hash(uint v)
        {
            uint h = (uint)((v * 2654435769l) >> (32 - HASH_LOG));
            return (ushort)(h & HASH_MASK);
        }

        // Copy literals (number of which fits within an instruction)
        static uint Literals(uint runs, byte[] src, uint srcOffset, byte[] dest, uint destOffset)
        {
            while (runs > MAX_COPY)
            {
                dest[destOffset++] = MAX_COPY - 1;
                Copy256(dest, destOffset, src, srcOffset);
                srcOffset += MAX_COPY;
                destOffset += MAX_COPY;
                runs -= MAX_COPY;
            }
            if (runs > 0)
            {
                dest[destOffset++] = (byte)(runs - 1);
                // Originally this uses Copy64(), but that copies 8 * runs bytes, which seems unnecessary
                //Copy64(dest, destOffset, src, srcOffset, runs);
                MemCpy(dest, destOffset, src, srcOffset, runs);
                destOffset += runs;
            }
            return destOffset;
        }

        // special case of memcpy: at most 32 bytes
        static void SmallCopy(byte[] dest, uint destOffset, byte[] src, uint srcOffset, uint count)
        {
            // We don't have any optimizations, so directly call MemCpy()
            MemCpy(dest, destOffset, src, srcOffset, count);
        }

        static uint CompressFinalize(uint runs, byte[] src, uint srcOffset, byte[] dest, uint destOffset)
        {
            while (runs > MAX_COPY)
            {
                dest[destOffset++] = MAX_COPY - 1;
                SmallCopy(dest, destOffset, src, srcOffset, MAX_COPY);
                srcOffset += MAX_COPY;
                destOffset += MAX_COPY;
                runs -= MAX_COPY;
            }
            if (runs > 0)
            {
                dest[destOffset++] = (byte)(runs - 1);
                SmallCopy(dest, destOffset, src, srcOffset, runs);
                destOffset += runs;
            }
            return destOffset;
        }

        // Write match for level 1
        static uint Level1Match(uint len, uint distance, byte[] op, uint opOffset)
        {
            --distance;
            while (len > MAX_LEN - 2)
            {
                op[opOffset++] = (byte)((7 << 5) | (distance >> 8));
                op[opOffset++] = MAX_LEN - 2 - 7 - 2;
                op[opOffset++] = (byte)distance;
                len -= MAX_LEN - 2;
            }
            if (len < 7)
            {
                op[opOffset++] = (byte)((len << 5) | (distance >> 8));
                op[opOffset++] = (byte)distance;
            }
            else
            {
                op[opOffset++] = (byte)((7 << 5) | (distance >> 8));
                op[opOffset++] = (byte)(len - 7);
                op[opOffset++] = (byte)distance;
            }
            return opOffset;
        }

        static int Level1Compress(byte[] input, byte[] output)
        {
            uint ipOffset = 0;
            uint ipBound = (uint)input.Length - 4;
            uint ipLimit = (uint)input.Length - 12 - 1;
            uint opOffset = 0;

            uint[] htab = new uint[HASH_SIZE];

            // we start with literal copy
            uint anchorOffset = ipOffset;
            ipOffset += 2;

            // main loop
            while (ipOffset < ipLimit)
            {
                uint seq;
                uint cmp;
                uint refOffset;
                uint distance;

                // find potential match
                do
                {
                    seq = ReadU32(input, ipOffset) & 0xffffff;
                    uint hash = Hash(seq);
                    refOffset = htab[hash];
                    htab[hash] = ipOffset;
                    distance = ipOffset - refOffset;
                    cmp = distance < MAX_L1_DISTANCE ? ReadU32(input, refOffset) & 0xffffff : 0x1000000;
                    if (ipOffset >= ipLimit) break;
                    ++ipOffset;
                } while (seq != cmp);

                if (ipOffset >= ipLimit) break;
                --ipOffset;

                if (ipOffset > anchorOffset)
                {
                    opOffset = Literals(ipOffset - anchorOffset, input, anchorOffset, output, opOffset);
                }

                uint len = Cmp(input, refOffset + 3, input, ipOffset + 3, ipBound);
                opOffset = Level1Match(len, distance, output, opOffset);

                // update the hash at match boundary
                ipOffset += len;
                seq = ReadU32(input, ipOffset);
                uint hash2 = Hash(seq & 0xffffff);
                htab[hash2] = ipOffset++;
                seq >>= 8;
                hash2 = Hash(seq);
                htab[hash2] = ipOffset++;

                anchorOffset = ipOffset;
            }

            uint copy = (uint)input.Length - anchorOffset;
            opOffset = CompressFinalize(copy, input, ipOffset, output, opOffset);
            return (int)opOffset;
        }

        static int Level1Decompress(byte[] input, byte[] output)
        {
            uint ipOffset = 0;
            uint ipLimit = (uint)input.Length;
            uint ipBound = ipLimit - 2;
            uint opOffset = 0;
            uint opLimit = (uint)output.Length;
            uint ctrl = (uint)(input[ipOffset++] & 0x1f);

            while (true)
            {
                if (ctrl >= 0x20)
                {
                    uint len = (ctrl >> 5) - 1;
                    uint ofs = (ctrl & 0x1f) << 8;
                    uint refOffset = opOffset - ofs - 1;
                    if (len == 7 - 1)
                    {
                        BoundCheck(ipOffset <= ipBound);
                        len += input[ipOffset++];
                    }
                    refOffset -= input[ipOffset++];
                    len += 3;
                    BoundCheck(opOffset + len <= opLimit);
                    BoundCheck(refOffset >= 0);
                    MemMove(output, opOffset, output, refOffset, len);
                    opOffset += len;
                }
                else
                {
                    ++ctrl;
                    BoundCheck(opOffset + ctrl <= opLimit);
                    BoundCheck(ipOffset + ctrl <= ipLimit);
                    MemCpy(output, opOffset, input, ipOffset, ctrl);
                    ipOffset += ctrl;
                    opOffset += ctrl;
                }

                if (ipOffset > ipBound) break;
                ctrl = input[ipOffset++];
            }

            return (int)opOffset;
        }

        // Write match for level 2
        static uint Level2Match(uint len, uint distance, byte[] op, uint opOffset)
        {
            --distance;
            if (distance < MAX_L2_DISTANCE)
            {
                if (len < 7)
                {
                    op[opOffset++] = (byte)((len << 5) | (distance >> 8));
                    op[opOffset++] = (byte)distance;
                }
                else
                {
                    op[opOffset++] = (byte)((7 << 5) | (distance >> 8));
                    for (len -= 7; len >= 0xff; len -= 0xff)
                        op[opOffset++] = 0xff;
                    op[opOffset++] = (byte)len;
                    op[opOffset++] = (byte)distance;
                }
            }
            else
            {
                // far away, but not yet in the another galaxy...
                if (len < 7)
                {
                    distance -= MAX_L2_DISTANCE;
                    op[opOffset++] = (byte)((len << 5) | 0x1f);
                    op[opOffset++] = 0xff;
                    op[opOffset++] = (byte)(distance >> 8);
                    op[opOffset++] = (byte)distance;
                }
                else
                {
                    distance -= MAX_L2_DISTANCE;
                    op[opOffset++] = (7 << 5) | 0x1f;
                    for (len -= 7; len >= 0xff; len -= 0xff)
                        op[opOffset++] = 0xff;
                    op[opOffset++] = (byte)len;
                    op[opOffset++] = 0xff;
                    op[opOffset++] = (byte)(distance >> 8);
                    op[opOffset++] = (byte)distance;
                }
            }
            return opOffset;
        }

        static int Level2Compress(byte[] input, byte[] output)
        {
            uint ipOffset = 0;
            uint ipBound = (uint)input.Length - 4;
            uint ipLimit = (uint)input.Length - 12 - 1;
            uint opOffset = 0;

            uint[] htab = new uint[HASH_SIZE];

            // we start with literal copy
            uint anchorOffset = ipOffset;
            ipOffset += 2;

            // main loop
            while (ipOffset < ipLimit)
            {
                uint seq;
                uint cmp;
                uint refOffset;
                uint distance;

                // find potential match
                do
                {
                    seq = ReadU32(input, ipOffset) & 0xffffff;
                    uint hash = Hash(seq);
                    refOffset = htab[hash];
                    htab[hash] = ipOffset;
                    distance = ipOffset - refOffset;
                    cmp = distance < MAX_FARDISTANCE ? ReadU32(input, refOffset) & 0xffffff : 0x1000000;
                    if (ipOffset >= ipLimit) break;
                    ++ipOffset;
                } while (seq != cmp);

                if (ipOffset >= ipLimit) break;
                --ipOffset;

                // far, needs at least 5-byte match
                if (distance >= MAX_L2_DISTANCE)
                {
                    if (input[refOffset + 3] != input[ipOffset + 3] || input[refOffset + 4] != input[ipOffset + 4])
                    {
                        ++ipOffset;
                        continue;
                    }
                }

                if (ipOffset > anchorOffset)
                {
                    opOffset = Literals(ipOffset - anchorOffset, input, anchorOffset, output, opOffset);
                }

                uint len = Cmp(input, refOffset + 3, input, ipOffset + 3, ipBound);
                opOffset = Level2Match(len, distance, output, opOffset);

                // update the hash at match boundary
                ipOffset += len;
                seq = ReadU32(input, ipOffset);
                uint hash2 = Hash(seq & 0xffffff);
                htab[hash2] = ipOffset++;
                seq >>= 8;
                hash2 = Hash(seq);
                htab[hash2] = ipOffset++;

                anchorOffset = ipOffset;
            }

            uint copy = (uint)input.Length - anchorOffset;
            opOffset = CompressFinalize(copy, input, ipOffset, output, opOffset);

            // marker for fastlz2
            output[0] |= 1 << 5;

            return (int)opOffset;
        }

        static int Level2Decompress(byte[] input, byte[] output)
        {
            uint ipOffset = 0;
            uint ipLimit = (uint)input.Length;
            uint ipBound = ipLimit - 2;
            uint opOffset = 0;
            uint opLimit = (uint)output.Length;
            uint ctrl = (uint)(input[ipOffset++] & 0x1f);

            while (true)
            {
                if (ctrl >= 0x20)
                {
                    uint len = (ctrl >> 5) - 1;
                    uint ofs = (ctrl & 0x1f) << 8;
                    uint refOffset = opOffset - ofs - 1;

                    byte code;
                    if (len == 7 - 1)
                    {
                        do
                        {
                            BoundCheck(ipOffset <= ipBound);
                            code = input[ipOffset++];
                            len += code;
                        } while (code == 0xff);
                    }
                    code = input[ipOffset++];
                    refOffset -= code;
                    len += 3;

                    // match from 16-bit distance
                    if (code == 0xff && ofs == 31 << 8)
                    {
                        BoundCheck(ipOffset < ipBound);
                        ofs = (uint)(input[ipOffset++] << 8);
                        ofs |= input[ipOffset++];
                        refOffset = opOffset - ofs - MAX_L2_DISTANCE - 1;
                    }

                    BoundCheck(opOffset + len <= opLimit);
                    BoundCheck(refOffset >= 0);
                    MemMove(output, opOffset, output, refOffset, len);
                    opOffset += len;
                }
                else
                {
                    ++ctrl;
                    BoundCheck(opOffset + ctrl <= opLimit);
                    BoundCheck(ipOffset + ctrl <= ipLimit);
                    MemCpy(output, opOffset, input, ipOffset, ctrl);
                    ipOffset += ctrl;
                    opOffset += ctrl;
                }

                if (ipOffset >= ipLimit) break;
                ctrl = input[ipOffset++];
            }

            return (int)opOffset;
        }

        #endregion
    }
}
