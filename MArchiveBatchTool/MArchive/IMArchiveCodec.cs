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

namespace GMWare.M2.MArchive
{
    /// <summary>
    /// Represents an MArchive codec.
    /// </summary>
    public interface IMArchiveCodec
    {
        /// <summary>
        /// Gets the magic value identifying the codec used.
        /// </summary>
        uint Magic { get; }
        /// <summary>
        /// Gets a decompression stream using the codec.
        /// </summary>
        /// <param name="inStream">The stream to decompress.</param>
        /// <returns>A stream that can be read from to decompress <paramref name="inStream"/>.</returns>
        Stream GetDecompressionStream(Stream inStream);
        /// <summary>
        /// Gets a compression stream using the codec.
        /// </summary>
        /// <param name="inStream">The stream to compress.</param>
        /// <returns>A stream that can be written to to compress <paramref name="inStream"/>.</returns>
        Stream GetCompressionStream(Stream inStream);
    }
}
