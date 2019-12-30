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

namespace GMWare.M2
{
    /// <summary>
    /// Miscellaneous utilities.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Copies <paramref name="count"/> bytes from <paramref name="input"/> to <paramref name="output"/>.
        /// </summary>
        /// <param name="input">The stream to copy from.</param>
        /// <param name="output">The stream to copy to.</param>
        /// <param name="count">The number of bytes to copy.</param>
        // Modified from https://stackoverflow.com/a/230141/1180879
        public static void CopyStream(Stream input, Stream output, int count)
        {
            byte[] buffer = new byte[81920];
            int read;
            while (count > 0 && (read = input.Read(buffer, 0, Math.Min(buffer.Length, count))) > 0)
            {
                output.Write(buffer, 0, read);
                count -= read;
            }
        }
    }
}
