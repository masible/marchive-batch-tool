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
using System.Text;
using System.IO;

namespace GMWare.M2.Psb.Writing
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
