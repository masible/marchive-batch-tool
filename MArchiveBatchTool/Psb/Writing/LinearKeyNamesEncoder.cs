using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    public class LinearKeyNamesEncoder : IKeyNamesEncoder
    {
        uint minCharIndex;
        uint nextAvailableIndexBeforeMinCharIndex;
        uint nextAvailableIndex;
        Queue<NameNode> breadthQueue = new Queue<NameNode>();

        public void Process(RegularNameNode root, int totalNodes)
        {
            // Can't implement this, the valueOffset being associated with the parent node
            // means I can't just throw all the characters next to each other
            throw new NotImplementedException();

            // Init root node
            root.ParentIndex = 0;
            root.ValueOffset = 1;

            // If no characters below, we're done
            if (root.Children.Count == 0) return;

            // Find out minimum starting char
            minCharIndex = root.Children.Keys.OrderBy(x => x).First();
            nextAvailableIndex = minCharIndex;

            // Walk the tree
            
        }

        void ProcessQueue()
        {
            while (breadthQueue.Count > 0)
            {
                var node = breadthQueue.Dequeue();
                node.Index = GetNextIndex(node is TerminalNameNode);
                if (node.Parent != null)
                {
                    node.ParentIndex = node.Parent.Index;
                }
                RegularNameNode regularNode = node as RegularNameNode;
                if (regularNode != null)
                {
                    foreach (var child in regularNode.Children.OrderBy(x => x.Key).Select(x => x.Value))
                        breadthQueue.Enqueue(child);
                }
            }
        }


        uint GetNextIndex(bool isTerminal)
        {
            if (nextAvailableIndexBeforeMinCharIndex < minCharIndex && isTerminal)
                return nextAvailableIndexBeforeMinCharIndex++;
            else
                return nextAvailableIndex++;
        }
    }
}
