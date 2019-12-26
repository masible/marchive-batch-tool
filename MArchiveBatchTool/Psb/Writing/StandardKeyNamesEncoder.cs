using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;

namespace MArchiveBatchTool.Psb.Writing
{
    /// <summary>
    /// Standard key names encoder that tries to fit ranges compactly.
    /// </summary>
    public class StandardKeyNamesEncoder : IKeyNamesEncoder
    {
        List<bool> usedRangeMap;
        uint minFreeSlot;
        uint maxFreeSlot;
        List<uint> cachedRange = new List<uint>();
        bool outputDebug;
        IndentedTextWriter writer;

        /// <inheritdoc/>
        public bool IsProcessed { get; private set; }

        /// <inheritdoc/>
        public int TotalSlots
        {
            get
            {
                if (!IsProcessed) throw new InvalidOperationException("Cannot get total slots until processed.");
                return usedRangeMap.Count;
            }
        }

        /// <summary>
        /// Gets or sets whether to write debug tree to standard output.
        /// </summary>
        public bool OutputDebug
        {
            get
            {
                return outputDebug;
            }
            set
            {
                outputDebug = value;
                if (outputDebug && writer == null)
                    writer = new IndentedTextWriter(Console.Out);
            }
        }

        /// <inheritdoc/>
        public void Process(RegularNameNode root, int totalNodes)
        {
            // Rules
            // 1. Character index must be high enough so valueOffset is at least 1.
            //    That means the index is at least char value + 1
            // 2. After applying above, find lowest index where the child characters
            //    would fit (i.e. none of the character slots desired are already
            //    occupied in the map)
            // 3. If a node has a terminating child, any non-terminating children are
            //    strictly based upon the current node's valueOffet
            // 4. Terminating node takes the first available index, unless it's child of
            //    a node that contains regular child nodes, in which case it is included
            //    in free range location (i.e. first child value is NUL, and remaining
            //    children are their byte values)
            // 5. Each node assigns index to terminating child first, then regular children,
            //    and after that child nodes are processed in depth-first order
            // 6. Gaps are OK

            IsProcessed = false;
            usedRangeMap = Enumerable.Repeat(false, totalNodes).ToList();

            // Special case: root always occupies 0
            root.Index = 0;
            usedRangeMap[0] = true;
            minFreeSlot = 1;
            maxFreeSlot = 1;

            ProcessNode(root);
            IsProcessed = true;
        }

        void ProcessNode(RegularNameNode currNode)
        {
            cachedRange.Clear();

            if (OutputDebug) writer.Write($"{currNode.Index} {(char)currNode.Character} ");

            var children = currNode.Children.Select(x => x.Value).OrderBy(x => x.Character).ToArray();
            // It's only possible for root node to not have children
            if (children.Length == 0)
            {
                if (OutputDebug) writer.WriteLine();
                return;
            }

            uint minChildValue = children[0].Character;
            foreach (var child in children)
            {
                cachedRange.Add(child.Character - minChildValue);
            }
            bool needExtending;
            uint minChildIndex = FindFreeRange(Math.Max(minFreeSlot, minChildValue + 1u), cachedRange, out needExtending, false);
            if (needExtending)
                ExtendRangeMap(minChildIndex + cachedRange[cachedRange.Count - 1]);

            for (int i = 0; i < children.Length; ++i)
            {
                var child = children[i];
                child.Index = minChildIndex + cachedRange[i];
                child.ParentIndex = currNode.Index;
                usedRangeMap[(int)child.Index] = true;
            }

            uint lastChildIndexFree = children[children.Length - 1].Index + 1;
            if (lastChildIndexFree > maxFreeSlot) maxFreeSlot = lastChildIndexFree;

            if (OutputDebug)
            {
                int i = 0;
                if (children[0] is TerminalNameNode)
                {
                    writer.Write($"[{children[0].Index}] ");
                    i = 1;
                }

                if (children.Length - i == 1)
                    writer.Write($"<{children[i].Index}>");
                else if (children.Length - i > 1)
                    writer.Write($"<{children[i].Index} {children[children.Length - 1].Index}>");
            }

            currNode.ValueOffset = minChildIndex - minChildValue;
            UpdateMinFreeSlot();

            if (OutputDebug)
            {
                writer.WriteLine();
                ++writer.Indent;
            }

            foreach (RegularNameNode child in children.Where(c => c is RegularNameNode))
                ProcessNode(child);

            if (OutputDebug) --writer.Indent;
        }

        void UpdateMinFreeSlot()
        {
            while (minFreeSlot < usedRangeMap.Count && usedRangeMap[(int)minFreeSlot])
                ++minFreeSlot;
        }

        uint FindFreeRange(uint minSlot, List<uint> range, out bool needExtending, bool hardConstraint)
        {
            needExtending = false;
            for (uint i = minSlot; i < usedRangeMap.Count; ++i)
            {
                bool found = true;
                foreach (uint o in range)
                {
                    uint target = i + o;
                    if (target < usedRangeMap.Count)
                    {
                        if (usedRangeMap[(int)target])
                        {
                            // Needed slot occupied, restart search at next slot
                            found = false;
                            if (hardConstraint) throw new Exception("Range not free for hard constraint");
                            break;
                        }
                    }
                    else
                    {
                        // Out of current map and all previous slots fulfilled, so quit
                        // early and indicate need extending
                        needExtending = true;
                        break;
                    }
                }
                // If it reaches here after completing foreach and still found, slots are fulfilled
                if (found)
                    return i;
            }

            // Can't find any free slots in current map, so we need to extend
            needExtending = true;
            return (uint)usedRangeMap.Count;
        }

        void ExtendRangeMap(uint targetIndex)
        {
            while (usedRangeMap.Count <= targetIndex)
                usedRangeMap.Add(false);
        }
    }
}
