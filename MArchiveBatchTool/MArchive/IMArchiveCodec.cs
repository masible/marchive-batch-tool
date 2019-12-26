using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MArchiveBatchTool.MArchive
{
    /// <summary>
    /// Represents an MArchive codec.
    /// </summary>
    public interface IMArchiveCodec
    {
        /// <summary>
        /// Gets the magic value identifying the codec used.
        /// </summary>
        uint Magic { get; }
        /// <summary>
        /// Gets a decompression stream using the codec.
        /// </summary>
        /// <param name="inStream">The stream to decompress.</param>
        /// <returns>A stream that can be read from to decompress <paramref name="inStream"/>.</returns>
        Stream GetDecompressionStream(Stream inStream);
        /// <summary>
        /// Gets a compression stream using the codec.
        /// </summary>
        /// <param name="inStream">The stream to compress.</param>
        /// <returns>A stream that can be written to to compress <paramref name="inStream"/>.</returns>
        Stream GetCompressionStream(Stream inStream);
    }
}
