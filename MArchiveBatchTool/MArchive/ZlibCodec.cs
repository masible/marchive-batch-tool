using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace GMWare.M2.MArchive
{
    /// <summary>
    /// Represents a Zlib codec.
    /// </summary>
    public class ZlibCodec : IMArchiveCodec
    {
        /// <inheritdoc/>
        public uint Magic => 0x0066646d; // "mdf\0"

        /// <inheritdoc/>
        public Stream GetCompressionStream(Stream inStream)
        {
            return new DeflaterOutputStream(inStream);
        }

        /// <inheritdoc/>
        public Stream GetDecompressionStream(Stream inStream)
        {
            return new InflaterInputStream(inStream);
        }
    }
}
