using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Represents a binary stream value.
    /// </summary>
    public class JStream : JValue
    {
        byte[] binaryDataBacking;
        uint index;
        bool isBStream;

        /// <summary>
        /// Gets the <see cref="PsbReader"/> this stream is associated with.
        /// </summary>
        internal PsbReader Reader { get; set; }
        /// <summary>
        /// Gets or sets the binary data for this stream.
        /// </summary>
        public byte[] BinaryData
        {
            get
            {
                if (binaryDataBacking == null && Reader != null)
                {
                    binaryDataBacking = Reader.GetStreamData(this);
                    Reader = null;
                }
                return binaryDataBacking;
            }
            set
            {
                binaryDataBacking = value;
                Reader = null;
            }
        }

        /// <summary>
        /// Gets or sets the index of this stream.
        /// </summary>
        public uint Index
        {
            get
            {
                return index;
            }
            internal set
            {
                index = value;
                UpdateName();
            }
        }

        /// <summary>
        /// Gets or sets whether this stream is a B-stream.
        /// </summary>
        public bool IsBStream
        {
            get
            {
                return isBStream;
            }
            internal set
            {
                isBStream = value;
                UpdateName();
            }
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="JStream"/>/
        /// </summary>
        /// <param name="isBStream">Whether this stream is a B-stream.</param>
        public JStream(bool isBStream) : base(string.Format("_{0}stream:new", isBStream ? "b" : ""))
        {
            this.isBStream = isBStream;
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="JStream"/> from a PSB.
        /// </summary>
        /// <param name="index">The index of this stream.</param>
        /// <param name="isBStream">Whether this stream is a B-stream.</param>
        /// <param name="parent">The reader this stream is associated with.</param>
        internal JStream(uint index, bool isBStream, PsbReader parent = null) : base(string.Empty)
        {
            this.index = index;
            this.isBStream = isBStream;
            Reader = parent;
            UpdateName();
        }

        void UpdateName()
        {
            Value = string.Format("_{0}stream:{1}", isBStream ? "b" : "", index);
        }

        /// <summary>
        /// Creates a new <see cref="JStream"/> based on a string representation.
        /// </summary>
        /// <param name="rep">The string representation.</param>
        /// <returns>The created <see cref="JStream"/>.</returns>
        /// <exception cref="ArgumentException">When <paramref name="rep"/> does not adhere to the expected representation.</exception>
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
