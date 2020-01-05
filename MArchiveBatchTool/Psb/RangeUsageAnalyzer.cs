// SPDX-License-Identifier: GPL-3.0-or-later
/*
 * GMWare.M2: Library for manipulating files in formats created by M2 Co., Ltd.
 * Copyright (C) 2019  Yukai Li
 * 
 * This file is part of GMWare.M2.
 * 
 * GMWare.M2 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * GMWare.M2 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with GMWare.M2.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GMWare.M2.Psb
{
#if DEBUG
    /// <summary>
    /// Generates key names range usage visualization.
    /// </summary>
    public class RangeUsageAnalyzer
    {
        /// <summary>
        /// Represents a range node.
        /// </summary>
        public class RangeNode
        {
            /// <summary>
            /// Gets or sets the parent node.
            /// </summary>
            public RangeNode Parent { get; set; }
            /// <summary>
            /// Gets or sets the node index.
            /// </summary>
            public uint Index { get; set; }
            /// <summary>
            /// Gets or sets the minimum value of child nodes.
            /// </summary>
            public uint Min { get; set; }
            /// <summary>
            /// Gets or sets the maximum value of child nodes.
            /// </summary>
            public uint Max { get; set; }
            /// <summary>
            /// Gets or sets the rank of this node.
            /// </summary>
            public int Rank { get; set; } = -1;
            /// <summary>
            /// Gets the set of child nodes.
            /// </summary>
            public HashSet<RangeNode> Children { get; } = new HashSet<RangeNode>();
            /// <summary>
            /// Gets or sets if node is terminal.
            /// </summary>
            public bool IsTerminal { get; set; }
            /// <summary>
            /// Gets whether node is singular (only has itself as value).
            /// </summary>
            public bool IsSingular => Min == Max;
        }

        List<RangeNode> nodeCache = new List<RangeNode>();
        uint maxSoFar;
        List<List<RangeNode>> coverageMap = new List<List<RangeNode>>();

        /// <summary>
        /// Gets whether nodes have been ordered.
        /// </summary>
        public bool IsOrdered { get; private set; }

        /// <summary>
        /// Adds a new range.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <param name="min">The minimum value of children.</param>
        /// <param name="max">The maximum value of children.</param>
        /// <param name="isTerminal">Whether this node is a terminal node.</param>
        public void AddRange(uint index, uint min, uint max, bool isTerminal)
        {
            if (min > max) throw new ArgumentException("min is greater than max");
            if (isTerminal && min != max)
                throw new ArgumentException("min and max are not the same for a terminal node.");
            nodeCache.Add(new RangeNode { Index = index, Min = min, Max = max, IsTerminal = isTerminal });
            if (max > maxSoFar) maxSoFar = max;
            IsOrdered = false;
        }

        /// <summary>
        /// Orders the nodes.
        /// </summary>
        public void OrderNodes()
        {
            // NOTE: The assumption is ranges do not cross parent ranges.
            // If they do, the results may be incorrect.

            // TODO: Rewrite this with the knowledge that terminal nodes are
            // not to be treated separately for nodes with both terminal and
            // child nodes. Not that this visualization is particularly
            // useful anymore.

            // Clear existing ordering
            IsOrdered = false;
            foreach (var node in nodeCache)
            {
                node.Parent = null;
                node.Children.Clear();
            }
            coverageMap.Clear();

            // Init new coverage map
            for (int i = 0; i <= maxSoFar; ++i)
            {
                coverageMap.Add(new List<RangeNode>());
            }

            // Fill up coverage map
            foreach (var node in nodeCache)
            {
                coverageMap[(int)node.Index].Add(node);
                for (uint i = node.Min; i <= node.Max; ++i)
                    coverageMap[(int)i].Add(node);
            }

            // Sort and link nodes
            Stack<RangeNode> nodeStack = new Stack<RangeNode>();
            for (int i = 0; i < coverageMap.Count; ++i)
            {
                coverageMap[i] = coverageMap[i].OrderBy(x => x.Min)
                    .ThenBy(x => !x.IsSingular).ToList();
                foreach (var node in coverageMap[i])
                {
                    // If stack empty or min greater than parent max, pop parent
                    // If node is non-singular, push to stack
                    RangeNode parent = null;
                    while (true)
                    {
                        if (nodeStack.Count > 0)
                        {
                            parent = nodeStack.Peek();
                            if (node.Min > node.Max)
                                nodeStack.Pop();
                            else
                                break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (node.Parent == null)
                    {
                        if (parent != null)
                        {
                            node.Parent = parent;
                            node.Rank = parent.Rank + 1;
                            parent.Children.Add(node);
                        }
                        else
                        {
                            node.Rank = 0;
                        }
                    }

                    if (!node.IsSingular) nodeStack.Push(node);
                }

                nodeStack.Clear();
            }

            IsOrdered = true;
        }

        /// <summary>
        /// Writes visualization.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <exception cref="InvalidOperationException">When the nodes have not been ordered yet.</exception>
        /// <exception cref="NullReferenceException">When <paramref name="writer"/> is <c>null</c>.</exception>
        public void WriteVisualization(TextWriter writer)
        {
            if (!IsOrdered) throw new InvalidOperationException("Nodes not ordered yet.");
            if (writer == null) throw new NullReferenceException(nameof(writer));

            // Write first line: which slots are occupied in the entire range
            char[] firstLine = Enumerable.Repeat('!', coverageMap.Count).ToArray();
            foreach (var node in nodeCache)
                firstLine[node.Index] = '*';
            writer.WriteLine(new string(firstLine));

            // Write each rank
            foreach (var rank in nodeCache.GroupBy(x => x.Rank).OrderBy(x => x.Key))
            {
                char[] line = Enumerable.Repeat(' ', coverageMap.Count).ToArray();
                foreach (var node in rank)
                {
                    if (node.IsTerminal && !node.IsSingular)
                        throw new Exception();
                    if (node.IsSingular)
                    {
                        if (node.IsTerminal)
                            line[node.Index] = '#';
                        else
                            line[node.Index] = '*';
                    }
                    else
                    {
                        line[node.Min] = '<';
                        line[node.Max] = '>';
                        if (line[node.Index] == ' ')
                            line[node.Index] = '*';
                    }
                }
                writer.WriteLine(new string(line));
            }
        }
    }
#endif
}
