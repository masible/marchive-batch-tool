using System;
using System.Collections.Generic;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
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

        public uint[] ValueOffsets
        {
            get
            {
                EnsureGenerated();
                return valueOffsets;
            }
        }

        public uint[] Tree
        {
            get
            {
                EnsureGenerated();
                return tree;
            }
        }

        public uint[] Tails
        {
            get
            {
                EnsureGenerated();
                return tails;
            }
        }

        public bool IsGenerated { get; private set; }

        public KeyNamesGenerator(IKeyNamesEncoder encoder)
        {
            this.encoder = encoder;
        }

        public void AddString(string s)
        {
            if (IsGenerated) throw new InvalidOperationException("Tree already generated.");
            if (s == null) throw new ArgumentNullException(nameof(s));
            strings.Add(s);
        }

        public void Generate()
        {
            // Can't be bothered to reset everything, so this is a one-time only operation
            if (IsGenerated) throw new InvalidOperationException("Tree already generated.");
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

        public uint this[string s]
        {
            get
            {
                EnsureGenerated();
                return stringLookup[s];
            }
        }
    }
}
