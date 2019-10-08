using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    public class StandardKeyNamesEncoder : IKeyNamesEncoder
    {
        List<bool> usedRangeMap;
        uint minFreeSlot;
        List<uint> cachedRange = new List<uint>();

        public bool IsProcessed { get; private set; }

        public int TotalSlots
        {
            get
            {
                if (!IsProcessed) throw new InvalidOperationException("Cannot get total slots until processed.");
                return usedRangeMap.Count;
            }
        }


        public void Process(RegularNameNode root, int totalNodes)
        {
            // Rules
            // 1. Character index must be high enough so valueOffset is at least 1.
            //    That means the index is at least char value + 1
            // 2. After applying above, find lowest index where the child characters
            //    would fit (i.e. none of the character slots desired are already
            //    occupied in the map)
            // 3. Terminating node takes the first available index
            // 4. If a node has a terminating child, any non-terminating children are
            //    strictly based upon the current node's valueOffet
            // 5. Each node assigns index to terminating child first, then regular children,
            //    and after that child nodes are processed in depth-first order
            // 6. Gaps are OK

            IsProcessed = false;
            usedRangeMap = Enumerable.Repeat(false, totalNodes).ToList();
            minFreeSlot = 0;

            // Special case: root always occupies 0
            root.Index = 0;
            usedRangeMap[0] = true;
            ++minFreeSlot;

            ProcessNode(root);
            IsProcessed = true;
        }

        void ProcessNode(RegularNameNode currNode)
        {
            cachedRange.Clear();

            // Process terminal node first; depending whether it's present, range may be modified
            TerminalNameNode terminalNode = currNode.Children.Select(x => x.Value)
                .FirstOrDefault(x => x is TerminalNameNode) as TerminalNameNode;
            if (terminalNode != null)
            {
                terminalNode.Index = minFreeSlot;
                terminalNode.ParentIndex = currNode.Index;
                currNode.ValueOffset = terminalNode.Index;
                ExtendRangeMap(terminalNode.Index);
                usedRangeMap[(int)terminalNode.Index] = true;
                UpdateMinFreeSlot();
            }

            var children = currNode.Children.Select(x => x.Value).Where(x => x is RegularNameNode)
                .Cast<RegularNameNode>().OrderBy(x => x.Character).ToArray();
            if (children.Length > 0)
            {
                uint minChildIndex = terminalNode != null ? children[0].Character + currNode.ValueOffset : Math.Max(minFreeSlot, children[0].Character + 1u);
                uint minChildValue = children[0].Character;
                foreach (var child in children)
                {
                    cachedRange.Add(child.Character - minChildValue);
                }

                bool needExtending;
                minChildIndex = FindFreeRange(minChildIndex, cachedRange, out needExtending, terminalNode != null);
                if (needExtending)
                    ExtendRangeMap(minChildIndex + cachedRange[cachedRange.Count - 1]);
                for (int i = 0; i < children.Length; ++i)
                {
                    var child = children[i];
                    child.Index = minChildIndex + cachedRange[i];
                    child.ParentIndex = currNode.Index;
                    usedRangeMap[(int)child.Index] = true;
                }

                if (terminalNode == null)
                    currNode.ValueOffset = children[0].Index - children[0].Character;
                UpdateMinFreeSlot();

                foreach (var child in children)
                    ProcessNode(child);
            }
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
