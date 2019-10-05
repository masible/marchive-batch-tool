using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Checksum;

namespace MArchiveBatchTool.Psb
{
    public class PsbReader : IDisposable
    {
        internal static readonly PsbTokenType[] IdToTypeMapping =
        {
            PsbTokenType.Invalid,
            PsbTokenType.Null,
            // true and false
            PsbTokenType.Bool, PsbTokenType.Bool,
            // 32-bit signed integer: value 0, 8 to 32 bits
            PsbTokenType.Int, PsbTokenType.Int, PsbTokenType.Int, PsbTokenType.Int, PsbTokenType.Int,
            // 64-bit signed integer: 40 to 64 buts
            PsbTokenType.Long, PsbTokenType.Long, PsbTokenType.Long, PsbTokenType.Long,
            // Array of unsigned ints, used for offsets
            PsbTokenType.UIntArray, PsbTokenType.UIntArray, PsbTokenType.UIntArray, PsbTokenType.UIntArray,
            // Keys index -> 32-bit unsigned integer, 8 to 32 bits
            PsbTokenType.Key, PsbTokenType.Key, PsbTokenType.Key, PsbTokenType.Key,
            // String index: 8 to 32 bits
            PsbTokenType.String, PsbTokenType.String, PsbTokenType.String, PsbTokenType.String,
            // Stream index: 8 to 32 bits
            PsbTokenType.Stream, PsbTokenType.Stream, PsbTokenType.Stream, PsbTokenType.Stream,
            // Single precision float: 0 or value
            PsbTokenType.Float, PsbTokenType.Float,
            PsbTokenType.Double,
            PsbTokenType.TokenArray,
            PsbTokenType.Object,
            // BStream index: 8 to 32 bits
            PsbTokenType.BStream, PsbTokenType.BStream, PsbTokenType.BStream, PsbTokenType.BStream,
        };

        Stream stream;
        BinaryReader br;
        IPsbFilter filter;
        KeyNamesReader keyNames;

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

        uint[] stringsOffsets;
        uint[] streamsOffsets;
        uint[] streamsSizes;
        uint[] bStreamsOffsets;
        uint[] bStreamsSizes;

        JToken root;

        public ushort Version { get; private set; }
        public PsbFlags Flags { get; private set; }
        public JToken Root
        {
            get
            {
                if (root == null)
                {
                    stream.Seek(rootOffset, SeekOrigin.Begin);
                    root = ReadTokenValue();
                }
                return root;
            }
        }
        public Dictionary<uint, JStream> StreamCache { get; } = new Dictionary<uint, JStream>();
        public Dictionary<uint, JStream> BStreamCache { get; } = new Dictionary<uint, JStream>();

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
            LoadKeyNames();
            LoadStringsOffsets();
            LoadStreamsInfo();
            if (Version >= 4) LoadBStreamsInfo();
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

        void LoadKeyNames()
        {
            keyNames = new KeyNamesReader(this);
        }

        void LoadStringsOffsets()
        {
            stream.Seek(stringsOffsetsOffset, SeekOrigin.Begin);
            stringsOffsets = ParseUIntArray(br.ReadByte());
        }

        void LoadStreamsInfo()
        {
            stream.Seek(streamsOffsetsOffset, SeekOrigin.Begin);
            streamsOffsets = ParseUIntArray(br.ReadByte());
            stream.Seek(streamsSizesOffset, SeekOrigin.Begin);
            streamsSizes = ParseUIntArray(br.ReadByte());
        }

        void LoadBStreamsInfo()
        {
            stream.Seek(bStreamsOffsetsOffset, SeekOrigin.Begin);
            bStreamsOffsets = ParseUIntArray(br.ReadByte());
            stream.Seek(bStreamsSizesOffset, SeekOrigin.Begin);
            bStreamsSizes = ParseUIntArray(br.ReadByte());
        }

        public void Close()
        {
            stream.Close();
        }

        public void Dispose()
        {
            Close();
        }

        #region Stream decoding functions
        JToken ReadTokenValue()
        {
            byte typeId = br.ReadByte();
            if (typeId < 0 || typeId >= IdToTypeMapping.Length)
                throw new InvalidDataException("Unknown type ID");
            switch (IdToTypeMapping[typeId])
            {
                case PsbTokenType.Null:
                    return JValue.CreateNull();
                case PsbTokenType.Bool:
                    return ParseBool(typeId);
                case PsbTokenType.Int:
                    return ParseInt(typeId);
                case PsbTokenType.Long:
                    return ParseLong(typeId);
                case PsbTokenType.UIntArray:
                    var arr = ParseUIntArray(typeId);
                    JArray jarr = new JArray();
                    foreach (var item in arr)
                    {
                        jarr.Add(item);
                    }
                    return jarr;
                case PsbTokenType.Key:
                    return ParseKey(typeId);
                case PsbTokenType.String:
                    return ParseString(typeId);
                case PsbTokenType.Stream:
                    return ParseStream(typeId);
                case PsbTokenType.Float:
                    return ParseFloat(typeId);
                case PsbTokenType.Double:
                    return ParseDouble(typeId);
                case PsbTokenType.TokenArray:
                    return ParseTokenArray(typeId);
                case PsbTokenType.Object:
                    return ParseObject(typeId);
                case PsbTokenType.BStream:
                    return ParseBStream(typeId);
                case PsbTokenType.Invalid:
                default:
                    throw new InvalidDataException("Invalid token type");
            }
        }

        bool ParseBool(byte typeId)
        {
            return typeId == 2;
        }

        int ParseInt(byte typeId)
        {
            switch (typeId)
            {
                case 4:
                    return 0;
                case 5:
                    return br.ReadSByte();
                case 6:
                    return br.ReadInt16();
                case 7:
                    return br.ReadUInt16() | (br.ReadSByte() << 16);
                case 8:
                    return br.ReadInt32();
                default:
                    throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));
            }
        }

        long ParseLong(byte typeId)
        {
            switch (typeId)
            {
                case 9:
                    return br.ReadUInt32() | (br.ReadSByte() << 32);
                case 10:
                    return br.ReadUInt32() | (br.ReadInt16() << 32);
                case 11:
                    return br.ReadUInt32() | (br.ReadUInt16() << 32) | (br.ReadSByte() << 48);
                case 12:
                    return br.ReadInt64();
                default:
                    throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));
            }
        }

        uint ParseUInt(byte typeId)
        {
            switch (typeId)
            {
                case 13:
                    return br.ReadByte();
                case 14:
                    return br.ReadUInt16();
                case 15:
                    return (uint)(br.ReadUInt16() | (br.ReadByte() << 16));
                case 16:
                    return br.ReadUInt32();
                default:
                    throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));
            }
        }

        uint[] ParseUIntArray(byte typeId)
        {
            uint count = ParseUInt(typeId);
            uint[] arr = new uint[count];
            byte valTypeId = br.ReadByte();
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = ParseUInt(valTypeId);
            }
            return arr;
        }

        string ParseKey(byte typeId)
        {
            uint keyIndex = ParseUInt((byte)(typeId - 4));
            return keyNames[keyIndex];
        }

        string ParseString(byte typeId)
        {
            uint stringIndex = ParseUInt((byte)(typeId - 8));
            long oldPos = stream.Position;
            stream.Seek(stringsBlobOffset + stringsOffsets[stringIndex], SeekOrigin.Begin);
            string s = ReadStringZ();
            stream.Position = oldPos;
            return s;
        }

        JStream ParseStream(byte typeId)
        {
            uint index = ParseUInt((byte)(typeId - 12));
            long oldPos = stream.Position;
            stream.Seek(streamsBlobOffset + streamsOffsets[index], SeekOrigin.Begin);
            byte[] data = br.ReadBytes((int)streamsSizes[index]);
            stream.Position = oldPos;
            var js = new JStream(index, false) { BinaryData = data };
            StreamCache[index] = js;
            return js;
        }

        float ParseFloat(byte typeId)
        {
            switch (typeId)
            {
                case 29:
                    return 0f;
                case 30:
                    return br.ReadSingle();
                default:
                    throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));
            }
        }

        double ParseDouble(byte typeId)
        {
            switch (typeId)
            {
                case 31:
                    return br.ReadDouble();
                default:
                    throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));
            }
        }

        JArray ParseTokenArray(byte typeId)
        {
            if (typeId != 32)
                throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));

            uint[] offsets = ParseUIntArray(br.ReadByte());
            long seekBase = stream.Position;
            JArray jarr = new JArray();
            for (int i = 0; i < offsets.Length; ++i)
            {
                stream.Seek(seekBase + offsets[i], SeekOrigin.Begin);
                jarr.Add(ReadTokenValue());
            }
            return jarr;
        }

        JObject ParseObject(byte typeId)
        {
            if (typeId != 33)
                throw new ArgumentException("Dispatched to incorrect parser.", nameof(typeId));

            uint[] keyIndexes = ParseUIntArray(br.ReadByte());
            uint[] offsets = ParseUIntArray(br.ReadByte());
            long seekBase = stream.Position;

            JObject obj = new JObject();
            for (int i = 0; i < keyIndexes.Length; ++i)
            {
                string keyName = keyNames[keyIndexes[i]];
                stream.Seek(seekBase + offsets[i], SeekOrigin.Begin);
                obj.Add(keyName, ReadTokenValue());
            }
            return obj;
        }

        JStream ParseBStream(byte typeId)
        {
            uint index = ParseUInt((byte)(typeId - 21));
            long oldPos = stream.Position;
            stream.Seek(bStreamsBlobOffset + bStreamsOffsets[index], SeekOrigin.Begin);
            byte[] data = br.ReadBytes((int)bStreamsSizes[index]);
            stream.Position = oldPos;
            var js = new JStream(index, true) { BinaryData = data };
            BStreamCache[index] = js;
            return js;
        }

        string ReadStringZ()
        {
            List<byte> buffer = new List<byte>();
            byte b = br.ReadByte();
            while (b != 0)
            {
                buffer.Add(b);
                b = br.ReadByte();
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }
        #endregion

        class KeyNamesReader
        {
            uint[] valueOffsets;
            uint[] tree;
            uint[] tails;
            Dictionary<uint, string> cache = new Dictionary<uint, string>();

            public KeyNamesReader(PsbReader reader)
            {
                if (reader.Version == 1)
                {
                    reader.stream.Seek(reader.keysOffsetsOffset, SeekOrigin.Begin);
                    uint[] offsets = reader.ParseUIntArray(reader.br.ReadByte());
                    for (uint i = 0; i < offsets.Length; ++i)
                    {
                        reader.stream.Seek(reader.keysBlobOffset + offsets[i], SeekOrigin.Begin);
                        cache.Add(i, reader.ReadStringZ());
                    }

                }
                else
                {
                    reader.stream.Seek(reader.keysBlobOffset, SeekOrigin.Begin);
                    valueOffsets = reader.ParseUIntArray(reader.br.ReadByte());
                    tree = reader.ParseUIntArray(reader.br.ReadByte());
                    tails = reader.ParseUIntArray(reader.br.ReadByte());
                }
            }

            public string this[uint index]
            {
                get
                {
                    if (cache.TryGetValue(index, out string v))
                    {
                        return v;
                    }

                    List<byte> buffer = new List<byte>();
                    uint current = tree[tails[index]];
                    while (current != 0)
                    {
                        uint parent = tree[current];
                        buffer.Add((byte)(current - valueOffsets[parent]));
                        current = parent;
                    }

                    buffer.Reverse();
                    string s = Encoding.UTF8.GetString(buffer.ToArray());
                    cache.Add(index, s);
                    return s;
                }
            }
        }
    }
}
