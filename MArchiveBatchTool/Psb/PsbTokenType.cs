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

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Represents the types of tokens in a PSB.
    /// </summary>
    enum PsbTokenType
    {
        /// <summary>
        /// An unknown token type.
        /// </summary>
        Invalid,
        /// <summary>
        /// The value <c>null</c>.
        /// </summary>
        Null,
        /// <summary>
        /// The values <c>true</c> and <c>false</c>.
        /// </summary>
        Bool,
        /// <summary>
        /// A signed 32-bit integer.
        /// </summary>
        Int,
        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
        Long,
        /// <summary>
        /// An unsigned 32-bit integer.
        /// </summary>
        UInt,
        /// <summary>
        /// Index into key names array (v1 only).
        /// </summary>
        Key,
        /// <summary>
        /// A string.
        /// </summary>
        String,
        /// <summary>
        /// A stream.
        /// </summary>
        Stream,
        /// <summary>
        /// A single precision floating point number.
        /// </summary>
        Float,
        /// <summary>
        /// A double precision floating point number.
        /// </summary>
        Double,
        /// <summary>
        /// An array of tokens.
        /// </summary>
        TokenArray,
        /// <summary>
        /// A dictionary of strings and tokens.
        /// </summary>
        Object,
        /// <summary>
        /// A B-stream.
        /// </summary>
        BStream
    }
}
