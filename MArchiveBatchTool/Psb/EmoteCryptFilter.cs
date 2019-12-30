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
    /// Default <see cref="IPsbFilter"/> implementation for E-mote motion files.
    /// </summary>
    public class EmoteCryptFilter : IPsbFilter
    {
        XorShift128 rand;
        uint buffer;
        int bytesLeft;

        /// <summary>
        /// Instantiates a new instance of <see cref="EmoteCryptFilter"/>.
        /// </summary>
        /// <param name="seed">The seed used to initialize the RNG.</param>
        public EmoteCryptFilter(uint seed)
        {
            rand = new XorShift128(seed);
        }

        /// <inheritdoc/>
        public void Filter(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (buffer == 0) // M2 bug: they're checking buffer instead of bytes left
                {
                    buffer = rand.Next();
                    bytesLeft = sizeof(uint);
                }

                data[i] ^= (byte)buffer;
                buffer >>= 8;
                --bytesLeft;
            }
        }
    }
}
