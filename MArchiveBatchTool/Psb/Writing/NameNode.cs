using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MArchiveBatchTool.Psb.Writing
{
    /// <summary>
    /// Represents a name node. This is an abstract class.
    /// </summary>
    public abstract class NameNode
    {
        /// <summary>
        /// Gets or sets the index of this node.
        /// </summary>
        public uint Index { get; set; }
        /// <summary>
        /// Gets or sets the parent index of this node.
        /// </summary>
        public uint ParentIndex { get; set; }
        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        public NameNode Parent { get; set; }
        /// <summary>
        /// Gets or sets the character represented by this node.
        /// </summary>
        public byte Character { get; set; }

        /// <summary>
        /// Writes this node to a GraphViz DOT file.
        /// </summary>
        /// <param name="writer">The writer for the DOT file.</param>
        public abstract void WriteDot(TextWriter writer);
    }
}
