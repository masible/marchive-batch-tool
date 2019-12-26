using System;
using System.Collections.Generic;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    /// <summary>
    /// Generates key names tree.
    /// </summary>
    class KeyNamesGenerator
    {
        List<string> strings = new List<string>();
        Dictionary<string, uint> stringLookup = new Dictionary<string, uint>();
        RegularNameNode root;
        List<NameNode> nodeCache = new List<NameNode>();
        IKeyNamesEncoder encoder;
        uint[] valueOffsets;
        uint[] tree;
        uint[] tails;

        /// <summary>
        /// Gets the offsets array.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the tree has not been generated or the tree is flat.</exception>
        public uint[] ValueOffsets
        {
            get
            {
                EnsureGenerated();
                EnsureIsNotFlat();
                return valueOffsets;
            }
        }

        /// <summary>
        /// Gets the parent pointers array.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the tree has not been generated or the tree is flat.</exception>
        public uint[] Tree
        {
            get
            {
                EnsureGenerated();
                EnsureIsNotFlat();
                return tree;
            }
        }

        /// <summary>
        /// Gets the tails array.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the tree has not been generated or the tree is flat.</exception>
        public uint[] Tails
        {
            get
            {
                EnsureGenerated();
                EnsureIsNotFlat();
                return tails;
            }
        }

        /// <summary>
        /// Gets whether the tree has been generated.
        /// </summary>
        public bool IsGenerated { get; private set; }
        /// <summary>
        /// Gets whether a flat array is to be generated instead of a tree.
        /// </summary>
        public bool IsFlat { get; private set; }
        /// <summary>
        /// Gets the number of key names in the generator.
        /// </summary>
        public int Count => strings.Count;

        /// <summary>
        /// Instantiates a new instance of <see cref="KeyNamesGenerator"/>.
        /// </summary>
        /// <param name="encoder">The encoder to use for ordering tree nodes.</param>
        /// <param name="isFlat">Whether to generate a flat array instead of a tree.</param>
        /// <exception cref="ArgumentNullException">When encoder is <c>null</c> if not generating flat array.</exception>
        public KeyNamesGenerator(IKeyNamesEncoder encoder, bool isFlat)
        {
            if (!isFlat && encoder == null)
                throw new ArgumentNullException(nameof(encoder), "Encoder not supplied when not flat.");
            this.encoder = encoder;
            IsFlat = isFlat;
        }

        /// <summary>
        /// Adds a key name into the generator.
        /// </summary>
        /// <param name="s">The key name to add.</param>
        /// <exception cref="InvalidOperationException">When the tree has already been generated.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="s"/> is <c>null</c>.</exception>
        public void AddString(string s)
        {
            if (IsGenerated) throw new InvalidOperationException("Tree already generated.");
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (!strings.Contains(s))
                strings.Add(s);
        }

        /// <summary>
        /// Generates the key names tree.
        /// </summary>
        /// <remarks>This is a one-time only operation.</remarks>
        public void Generate()
        {
            // Can't be bothered to reset everything, so this is a one-time only operation
            if (IsGenerated) throw new InvalidOperationException("Tree already generated.");
            if (!IsFlat)
            {
                // 1. Sort strings
                strings.Sort((x, y) => string.CompareOrdinal(x, y));
                // 2. Make root
                root = GetOrCreateRegularNameNode(null, 0);
                // 3. Build the character tree
                for (uint i = 0; i < strings.Count; ++i)
                    InsertStringToTree(strings[(int)i], i);
                // 4. Use encoder to fill out indexes and stuff
                encoder.Process(root, nodeCache.Count);
                // 5. Create the output arrays
                valueOffsets = new uint[encoder.TotalSlots];
                tree = new uint[encoder.TotalSlots];
                tails = new uint[strings.Count];
                foreach (var node in nodeCache)
                {
                    tree[node.Index] = node.ParentIndex;
                    var regularNode = node as RegularNameNode;
                    if (regularNode != null)
                        valueOffsets[regularNode.Index] = regularNode.ValueOffset;
                    else
                    {
                        var termNode = node as TerminalNameNode;
                        tails[termNode.TailIndex] = termNode.Index;
                        valueOffsets[termNode.Index] = termNode.TailIndex;
                    }
                }
            }
            else
            {
                // Sort by length to reduce offset table size
                strings.Sort((x, y) => x.Length.CompareTo(y.Length));
            }
            // 6. Generate lookup
            for (uint i = 0; i < strings.Count; ++i)
                stringLookup.Add(strings[(int)i], i);
            IsGenerated = true;
        }

        void InsertStringToTree(string s, uint index)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            RegularNameNode currNode = root;
            foreach (byte b in bytes)
                currNode = GetOrCreateRegularNameNode(currNode, b);
            CreateTerminalNameNode(currNode, index);
        }

        RegularNameNode GetOrCreateRegularNameNode(RegularNameNode parent, byte ch)
        {
            NameNode node;
            if (parent != null)
            {
                if (!parent.Children.TryGetValue(ch, out node))
                {
                    node = new RegularNameNode();
                    node.Parent = parent;
                    node.Character = ch;
                    nodeCache.Add(node);
                    parent.Children.Add(ch, node);
                }
            }
            else
            {
                node = new RegularNameNode();
                node.Character = ch;
                nodeCache.Add(node);
            }
            return (RegularNameNode)node;
        }

        TerminalNameNode CreateTerminalNameNode(RegularNameNode parent, uint stringIndex)
        {
            var node = new TerminalNameNode();
            node.Parent = parent;
            node.Character = 0;
            node.TailIndex = stringIndex;
            nodeCache.Add(node);
            parent.Children.Add(0, node);
            return node;
        }

        void EnsureGenerated()
        {
            if (!IsGenerated) throw new InvalidOperationException("Tree has not been generated.");
        }

        void EnsureIsNotFlat()
        {
            if (IsFlat) throw new InvalidOperationException("Only available when not flat.");
        }

        /// <summary>
        /// Gets the index of a key name.
        /// </summary>
        /// <param name="s">The key name to get the index of.</param>
        /// <returns>The index corresponding to <paramref name="s"/>.</returns>
        public uint this[string s]
        {
            get
            {
                EnsureGenerated();
                return stringLookup[s];
            }
        }

        /// <summary>
        /// Gets the key name at <paramref name="i"/>.
        /// </summary>
        /// <param name="i">The index.</param>
        /// <returns>The key name.</returns>
        public string this[int i]
        {
            get
            {
                return strings[i];
            }
        }
    }
}
