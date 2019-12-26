using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MArchiveBatchTool
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
