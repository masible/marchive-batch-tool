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
        public Dictionary<string, List<int>> FileInfo { get; set; }
        /// <summary>
        /// Gets or sets the expire suffixes list.
        /// </summary>
        /// <remarks>Not sure what this is actually for.</remarks>
        [JsonProperty("expire_suffix_list")]
        public List<string> ExpireSuffixList { get; set; }
    }
}
