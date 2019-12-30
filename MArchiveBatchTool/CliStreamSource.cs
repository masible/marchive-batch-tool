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
using GMWare.M2.Psb;

namespace GMWare.M2
{
    /// <summary>
    /// Represents the <see cref="IPsbStreamSource"/> implementation used by this
    /// library's command line app.
    /// </summary>
    public class CliStreamSource : IPsbStreamSource
    {
        string baseDir;

        /// <summary>
        /// Instantiates a new instance of <see cref="CliStreamSource"/>.
        /// </summary>
        /// <param name="baseDir">The base directory to locate stream files from.</param>
        public CliStreamSource(string baseDir)
        {
            if (string.IsNullOrEmpty(baseDir)) throw new ArgumentNullException(nameof(baseDir));
            this.baseDir = baseDir;
        }

        /// <inheritdoc/>
        public Stream GetStream(string identifier)
        {
            string postproc = identifier.TrimStart('_').Replace(':', '_');
            return File.OpenRead(Path.Combine(baseDir, postproc));
        }
    }
}
