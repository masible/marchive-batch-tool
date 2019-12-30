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
    /// Random number generator used in E-mote with custom W seed
    /// 
    /// See http://dx.doi.org/10.18637/jss.v008.i14, page 5, xor128()
    /// </summary>
    class XorShift128
    {
        uint x = 123456789;
        uint y = 362436069;
        uint z = 521288629;
        uint w;

        /// <summary>
        /// Instantiates a new instance of <see cref="XorShift128"/>.
        /// </summary>
        /// <param name="seed">The seed to use to initialize the RNG.</param>
        public XorShift128(uint seed = 88675123)
        {
            w = seed;
        }

        /// <summary>
        /// Gets the next random value.
        /// </summary>
        /// <returns>A random unsigned 32-bit integer.</returns>
        public uint Next()
        {
            uint t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            return w;
        }
    }
}
