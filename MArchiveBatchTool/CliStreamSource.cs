using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MArchiveBatchTool.Psb;

namespace MArchiveBatchTool
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
