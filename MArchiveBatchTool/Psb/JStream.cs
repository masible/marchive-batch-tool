using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MArchiveBatchTool.Psb
{
    public class JStream : JValue
    {
        byte[] binaryDataBacking;

        internal PsbReader Reader { get; set; }
        public byte[] BinaryData
        {
            get
            {
                if (binaryDataBacking == null && Reader != null)
                {
                    return Reader.GetStreamData(this);
                }
                else
                {
                    return binaryDataBacking;
                }
            }
            set
            {
                binaryDataBacking = value;
            }
        }

        public uint Index { get; internal set; }
        public bool IsBStream { get; internal set; }

        public JStream(uint index, bool isBStream) : base(string.Empty)
        {
            Value = string.Format("_{0}stream:{1}", IsBStream ? "b" : "", index);
        }

        internal JStream(uint index, bool isBStream, PsbReader parent) : this(index, isBStream)
        {
            Reader = parent;
        }

        public static JStream CreateFromStringRepresentation(string rep)
        {
            string[] split = rep.Split(':');
            if (split.Length != 2 || split[0] != "_stream" && split[0] != "_bstream")
                throw new ArgumentException("String is not stream representation.", nameof(rep));
            uint index = uint.Parse(split[1]);
            bool isBStream = split[0] == "_bstream";
            return new JStream(index, isBStream);
        }
    }
}
