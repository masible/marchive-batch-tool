using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MArchiveBatchTool
{
    public interface IMArchiveCodec
    {
        uint Magic { get; }
        Stream GetDecompressionStream(Stream inStream);
        Stream GetCompressionStream(Stream inStream);
    }
}
