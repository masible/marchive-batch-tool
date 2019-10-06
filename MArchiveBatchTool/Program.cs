using System;
using System.IO;
using MArchiveBatchTool.MArchive;
using MArchiveBatchTool.Psb;
using Newtonsoft.Json;

namespace MArchiveBatchTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Pass me path to alldata.psb.m");
            }

            try
            {
                string archPath = args[0];
                string extractPath = archPath + "_extracted";
                var packer = new MArchivePacker(new ZStandardCodec(), "nY/RHn+XH8T77", 0x40);
                AllDataPacker.UnpackFiles(archPath, extractPath, packer);
                packer.DecompressDirectory(extractPath);

                foreach (string file in Directory.GetFiles(extractPath, "*.psb", SearchOption.AllDirectories))
                {
                    // IIRC there was some kind of bug with globbing, so double check extension
                    if (Path.GetExtension(file).ToLower() != ".psb") continue;
                    Console.WriteLine($"Deserializing {file}");
                    DumpPsb(file, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something went wrong: {ex.Message}");
            }
        }

        static void DumpPsb(string psbPath, bool writeDebug, IPsbFilter filter = null)
        {
            using (FileStream fs = File.OpenRead(psbPath))
            using (StreamWriter debugWriter = writeDebug ? File.CreateText(psbPath + ".debug.txt") : null)
            using (PsbReader psbReader = new PsbReader(fs, filter, debugWriter))
            {
                Newtonsoft.Json.Linq.JToken root;
                try
                {
                    root = psbReader.Root;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing PSB");
                    Console.WriteLine("Current position: 0x{0:x8}", fs.Position);
                    Console.WriteLine(ex);

                    if (writeDebug)
                    {
                        using (FileStream dcStream = File.Create(psbPath + ".decrypted"))
                        {
                            psbReader.DumpDecryptedStream(dcStream);
                        }
                    }
                    return;
                }
                using (StreamWriter sw = File.CreateText(psbPath + ".json"))
                using (JsonTextWriter jtw = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
                {
                    root.WriteTo(jtw);
                }
                if (psbReader.StreamCache.Count > 0 || psbReader.BStreamCache.Count > 0)
                {
                    string streamsDirPath = psbPath + ".streams";
                    Directory.CreateDirectory(streamsDirPath);
                    foreach (var js in psbReader.StreamCache)
                    {
                        File.WriteAllBytes(Path.Combine(streamsDirPath, "stream_" + js.Key), js.Value.BinaryData);
                    }
                    foreach (var js in psbReader.BStreamCache)
                    {
                        File.WriteAllBytes(Path.Combine(streamsDirPath, "bstream_" + js.Key), js.Value.BinaryData);
                    }
                }
            }
        }
    }
}
