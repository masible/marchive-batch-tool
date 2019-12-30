using System;
using System.Collections.Generic;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    /// <summary>
    /// Represents a key names encoder.
    /// </summary>
    /// <remarks>
    /// An encoder sets the index and offset on each name node, providing the necessary
    /// information to create a serialized name node tree.
    /// </remarks>
    public interface IKeyNamesEncoder
    {
        /// <summary>
        /// Gets whether processing has completed.
        /// </summary>
        bool IsProcessed { get; }
        /// <summary>
        /// Gets the total number of slots used after processing the tree.
        /// </summary>
        int TotalSlots { get; }
        /// <summary>
        /// Processes the tree provided by <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root node of the key names tree.</param>
        /// <param name="totalNodes">The total number of nodes in the tree.</param>
        void Process(RegularNameNode root, int totalNodes);
    }
}
