using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MArchiveBatchTool.Psb;
using MArchiveBatchTool.Models;

namespace MArchiveBatchTool.MArchive
{
    public static class AllDataPacker
    {
        public static void UnpackFiles(string psbPath, string outputPath, MArchivePacker maPacker = null)
        {
            // Figure out what file we've been given
            if (Path.GetExtension(psbPath).ToLower() == ".bin")
                psbPath = Path.ChangeExtension(psbPath, ".psb");

            // No PSB, try .psb.m
            if (!File.Exists(psbPath)) psbPath += ".m";

            if (Path.GetExtension(psbPath).ToLower() == ".m")
            {
                // Decompress .m file
                if (maPacker == null) throw new ArgumentNullException(nameof(maPacker));
                maPacker.DecompressFile(psbPath, true);
                psbPath = Path.ChangeExtension(psbPath, null);
            }

            ArchiveV1 arch;
            using (FileStream fs = File.OpenRead(psbPath))
            using (PsbReader reader = new PsbReader(fs))
            {
                var root = reader.Root;
                arch = root.ToObject<ArchiveV1>();
            }

            if (arch.ObjectType != "archive" || arch.Version != 1.0)
                throw new InvalidDataException("PSB file is not an archive.");

            using (FileStream fs = File.OpenRead(Path.ChangeExtension(psbPath, ".bin")))
            {
                foreach (var file in arch.FileInfo)
                {
                    Console.WriteLine($"Extracting {file.Key}");
                    string outPath = Path.Combine(outputPath, file.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    using (FileStream ofs = File.Create(outPath))
                    {
                        fs.Seek(file.Value[0], SeekOrigin.Begin);
                        Utils.CopyStream(fs, ofs, file.Value[1]);
                    }
                }
            }
        }
    }
}
