using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MArchiveBatchTool
{
    public static class Utils
    {
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
