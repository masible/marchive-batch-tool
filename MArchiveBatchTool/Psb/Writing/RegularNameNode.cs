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
using System.IO;
using System.Text;

namespace GMWare.M2.Psb.Writing
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
