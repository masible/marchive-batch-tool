using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Checksum;

namespace MArchiveBatchTool.Psb
{
    public class PsbReader
    {
        Stream stream;
        BinaryReader br;
        IPsbFilter filter;

        // Header values
        uint keysOffsetsOffset;
        uint keysBlobOffset;
        uint stringsOffsetsOffset;
        uint stringsBlobOffset;
        uint streamsOffsetsOffset;
        uint streamsSizesOffset;
        uint streamsBlobOffset;
        uint rootOffset;
        uint bStreamsOffsetsOffset;
        uint bStreamsSizesOffset;
        uint bStreamsBlobOffset;

        public ushort Version { get; private set; }
        public PsbFlags Flags { get; private set; }
        public JObject Root { get; private set; }

        public PsbReader(Stream stream, IPsbFilter filter)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanSeek) throw new ArgumentException("Stream cannot be seeked.", nameof(stream));
            this.stream = stream;
            br = new BinaryReader(stream);
            this.filter = filter;

            if (new string(br.ReadChars(4)) != "PSB\0") throw new InvalidDataException("File is not a PSB");
            Version = br.ReadUInt16();
            Flags = (PsbFlags)br.ReadUInt16();

            byte[] headerBytes = VerifyHeader();
            ReadHeader(headerBytes);
            SetupUnfiltering();
        }

        byte[] VerifyHeader()
        {
            int headerLength = 4 * 8;
            if (Version >= 3)
            {
                headerLength += 4;
                if (Version >= 4) headerLength += 3 * 4;
            }
            byte[] headerBytes = br.ReadBytes(headerLength);

            if (Version >= 3)
            {
                if (filter != null && (Flags & PsbFlags.HeaderFiltered) != 0) filter.Filter(headerBytes);
                uint headerChecksum = BitConverter.ToUInt32(headerBytes, 0x20);

                // This is probably actually for checking that the header was decrypted successfully
                Adler32 adler = new Adler32();
                adler.Update(new ArraySegment<byte>(headerBytes, 0, 0x20));
                if (Version >= 4) adler.Update(new ArraySegment<byte>(headerBytes, 0x24, 0x0c));
                if (adler.Value != headerChecksum) throw new InvalidDataException("Header checksum mismatch");
            }

            return headerBytes;
        }

        void ReadHeader(byte[] headerBytes)
        {
            using (MemoryStream ms = new MemoryStream(headerBytes))
            {
                BinaryReader hbr = new BinaryReader(ms);
                keysOffsetsOffset = hbr.ReadUInt32();
                keysBlobOffset = hbr.ReadUInt32();
                stringsOffsetsOffset = hbr.ReadUInt32();
                stringsBlobOffset = hbr.ReadUInt32();
                streamsOffsetsOffset = hbr.ReadUInt32();
                streamsSizesOffset = hbr.ReadUInt32();
                streamsBlobOffset = hbr.ReadUInt32();
                rootOffset = hbr.ReadUInt32();
                if (Version >= 3)
                {
                    hbr.ReadUInt32(); // Skip checksum
                    if (Version >= 4)
                    {
                        bStreamsOffsetsOffset = hbr.ReadUInt32();
                        bStreamsSizesOffset = hbr.ReadUInt32();
                        bStreamsBlobOffset = hbr.ReadUInt32();
                    }
                }
            }
        }

        void SetupUnfiltering()
        {
            if (filter != null && (Version < 3 || (Flags & PsbFlags.BodyFiltered) != 0))
            {
                stream = new OverlayReadStream(
                    stream,
                    keysOffsetsOffset,
                    Version >= 4 ? bStreamsOffsetsOffset : streamsOffsetsOffset,
                    filter
                );
                br = new BinaryReader(stream);
            }
        }
    }
}
