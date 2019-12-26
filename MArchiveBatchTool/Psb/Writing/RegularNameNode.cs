using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    /// <summary>
    /// Represents a regular name node.
    /// </summary>
    public class RegularNameNode : NameNode
    {
        /// <summary>
        /// Gets or sets the offset between the index of the lowest child and its value.
        /// </summary>
        public uint ValueOffset { get; set; }
        /// <summary>
        /// Gets the children of this node. The key is the value of the corresponding node.
        /// </summary>
        public IDictionary<byte, NameNode> Children { get; } = new Dictionary<byte, NameNode>();

        /// <inheritdoc/>
        public override void WriteDot(TextWriter writer)
        {
            string outputChar;
            char ch = (char)Character;
            if (char.IsControl(ch) || char.IsWhiteSpace(ch))
                outputChar = string.Format("0x{0:x2}", Character);
            else
                outputChar = ch.ToString();

            if (Index != 0)
            {
                writer.WriteLine("{0} [label=\"{{{{index|\\<{1}\\>}}|{{valueOffset|{2}}}|{{char|{3}}}}}\"];",
                Index, Index, ValueOffset, outputChar);
                writer.WriteLine("{1} -> {0};", Index, ParentIndex);
            }
            else
            {
                writer.WriteLine("{0} [label=\"{{{{index|\\<{1}\\>}}|{{valueOffset|{2}}}}}\"];", Index, Index, ValueOffset);
            }
        }
    }
}
