using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Zstandard.Net;

namespace GMWare.M2.MArchive
{
    /// <summary>
    /// Represents a ZStandard codec.
    /// </summary>
    public class ZStandardCodec : IMArchiveCodec
    {
        /// <inheritdoc/>
        public uint Magic => 0x00737a6d; // "mzs\0"

        /// <inheritdoc/>
        public Stream GetCompressionStream(Stream inStream)
        {
            return new ZstandardStream(inStream, CompressionMode.Compress, true);
        }

        /// <inheritdoc/>
        public Stream GetDecompressionStream(Stream inStream)
        {
            return new ZstandardStream(inStream, CompressionMode.Decompress, true);
        }
    }
}
