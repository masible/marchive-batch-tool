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
using Newtonsoft.Json;

namespace GMWare.M2.Models
{
    /// <summary>
    /// Represents the data model for an archive manifest.
    /// </summary>
    public class ArchiveV1
    {
        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonProperty("id")]
        public string ObjectType { get; set; }
        /// <summary>
        /// Gets or sets the version of the archive.
        /// </summary>
        [JsonProperty("version")]
        public float Version { get; set; }
        /// <summary>
        /// Gets or sets the list of files.
        /// </summary>
        /// <remarks>
        /// The key is the file path. The first <c>int</c> is the offset of the
        /// file, and the second <c>int</c> is the length of the file.
        /// </remarks>
        [JsonProperty("file_info")]
        public Dictionary<string, List<long>> FileInfo { get; set; }
        /// <summary>
        /// Gets or sets the expire suffixes list.
        /// </summary>
        /// <remarks>Not sure what this is actually for.</remarks>
        [JsonProperty("expire_suffix_list")]
        public List<string> ExpireSuffixList { get; set; }
    }
}
