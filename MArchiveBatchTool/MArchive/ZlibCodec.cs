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
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace GMWare.M2.MArchive
{
    /// <summary>
    /// Represents a Zlib codec.
    /// </summary>
    public class ZlibCodec : IMArchiveCodec
    {
        /// <inheritdoc/>
        public uint Magic => 0x0066646d; // "mdf\0"

        /// <inheritdoc/>
        public Stream GetCompressionStream(Stream inStream)
        {
            return new DeflaterOutputStream(inStream);
        }

        /// <inheritdoc/>
        public Stream GetDecompressionStream(Stream inStream)
        {
            return new InflaterInputStream(inStream);
        }
    }
}
