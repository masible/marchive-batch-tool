using System;
using System.IO;
using MArchiveBatchTool.MArchive;
using MArchiveBatchTool.Psb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace MArchiveBatchTool
{
    class Program
    {
        static readonly string[] AVAILABLE_CODECS = { "zstd", "zstandard", "zlib" };

        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = Path.GetFileName(Environment.GetCommandLineArgs()[0]),
                FullName = "MArchive Batch Tool"
            };

            app.Command("m", (config) =>
            {
                config.FullName = "Manipulate .m files";
                config.Description = "Unpacks/repacks .m files; call directly to automatically pack/unpack file as appropriate";

                config.Command("unpack", (innerConfig) =>
                {
                    innerConfig.FullName = "Unpack .m";
                    innerConfig.Description = "Unpacks a .m file";
                    var pathArgL = innerConfig.Argument("path", "Path to the file or directory").IsRequired();
                    pathArgL.Accepts().ExistingFileOrDirectory();
                    var codecArgL = innerConfig.Argument("codec", "The codec used for packing/unpacking").IsRequired();
                    codecArgL.Accepts().Values(AVAILABLE_CODECS);
                    var keyArgL = innerConfig.Argument("seed", "The static seed").IsRequired(allowEmptyStrings: true);
                    var keyLengthArgL = innerConfig.Argument<int>("keyLength", "The key cycle length").IsRequired();
                    var keepOptionL = innerConfig.Option("--keep", "Keep packed file", CommandOptionType.NoValue);

                    innerConfig.OnExecute(() =>
                    {
                        var packer = new MArchivePacker(GetCodec(codecArgL.Value), keyArgL.Value, keyLengthArgL.ParsedValue);
                        if (File.Exists(pathArgL.Value))
                        {
                            packer.DecompressFile(pathArgL.Value, keepOptionL.HasValue());
                        }
                        else
                        {
                            packer.DecompressDirectory(pathArgL.Value, keepOptionL.HasValue());
                        }
                    });

                    innerConfig.HelpOption();
                });

                config.Command("pack", (innerConfig) =>
                {
                    innerConfig.FullName = "Pack .m";
                    innerConfig.Description = "Packs a file to .m";
                    var pathArgL = innerConfig.Argument("path", "Path to the file or directory").IsRequired();
                    pathArgL.Accepts().ExistingFileOrDirectory();
                    var codecArgL = innerConfig.Argument("codec", "The codec used for packing/unpacking").IsRequired();
                    codecArgL.Accepts().Values(AVAILABLE_CODECS);
                    var keyArgL = innerConfig.Argument("seed", "The static seed").IsRequired(allowEmptyStrings: true);
                    var keyLengthArgL = innerConfig.Argument<int>("keyLength", "The key cycle length").IsRequired();
                    var keepOptionL = innerConfig.Option("--keep", "Keep unpacked file", CommandOptionType.NoValue);

                    innerConfig.OnExecute(() =>
                    {
                        var packer = new MArchivePacker(GetCodec(codecArgL.Value), keyArgL.Value, keyLengthArgL.ParsedValue);
                        if (File.Exists(pathArgL.Value))
                        {
                            packer.CompressFile(pathArgL.Value, keepOptionL.HasValue());
                        }
                        else
                        {
                            packer.CompressDirectory(pathArgL.Value, keepOptionL.HasValue());
                        }
                    });

                    innerConfig.HelpOption();
                });

                //var pathArg = config.Argument("path", "Path to the file").IsRequired();
                //pathArg.Accepts().ExistingFile();
                //var codecArg = config.Argument("codec", "The codec used for packing/unpacking").IsRequired();
                //codecArg.Accepts().Values(AVAILABLE_CODECS);
                //var keyArg = config.Argument("seed", "The static seed").IsRequired(allowEmptyStrings: true);
                //var keyLengthArg = config.Argument<int>("keyLength", "The key cycle length").IsRequired();
                //var keepOption = config.Option("--keep", "Keep input file", CommandOptionType.NoValue);

                //config.OnExecute(() =>
                //{
                //    var packer = new MArchivePacker(GetCodec(codecArg.Value), keyArg.Value, keyLengthArg.ParsedValue);
                //    if (Path.GetExtension(pathArg.Value).ToLower() == ".m")
                //    {
                //        packer.DecompressFile(pathArg.Value, keepOption.HasValue());
                //    }
                //    else
                //    {
                //        packer.CompressFile(pathArg.Value, keepOption.HasValue());
                //    }
                //});

                config.HelpOption();

                config.OnExecute(() =>
                {
                    config.ShowHelp();
                });
            });

            app.Command("psb", (config) =>
            {
                config.FullName = "Manipulate .psb";
                config.Description = "Serializes/unserializes .psb files; call directly to serialize .json or deserialize .psb";

                config.Command("deserialize", (innerConfig) =>
                {
                    innerConfig.FullName = "Deserialize .psb";
                    innerConfig.Description = "Deserializes .psb file to .json and steam files";
                    var pathArgL = innerConfig.Argument("path", "Path of the .psb file").IsRequired();
                    pathArgL.Accepts().ExistingFile();
                    var keyOptionL = innerConfig.Option<uint>("--key", "Seed for Emote encryption filter", CommandOptionType.SingleValue);
                    var debugOptionL = innerConfig.Option("--debug", "Write debug files", CommandOptionType.NoValue);

                    innerConfig.OnExecute(() =>
                    {
                        IPsbFilter filter = null;
                        if (keyOptionL.HasValue()) filter = new EmoteCryptFilter(keyOptionL.ParsedValue);
                        DumpPsb(pathArgL.Value, debugOptionL.HasValue(), filter);
                    });

                    innerConfig.HelpOption();
                });

                config.Command("serialize", (innerConfig) =>
                {
                    innerConfig.FullName = "Serialize .json";
                    innerConfig.Description = "Serializes .json file to .psb";
                    var pathArgL = innerConfig.Argument("path", "Path of the .json file").IsRequired();
                    pathArgL.Accepts().ExistingFile();
                    var versionOption = innerConfig.Option<ushort>("--version", "PSB version (default 4)", CommandOptionType.SingleValue);
                    var filterHeaderOption = innerConfig.Option("--filterHeader", "Enable header filtering", CommandOptionType.NoValue);
                    var filterBodyOption = innerConfig.Option("--filterBody", "Enable body filtering", CommandOptionType.NoValue);
                    var keyOptionL = innerConfig.Option<uint>("--key", "Seed for Emote encryption filter", CommandOptionType.SingleValue);
                    var noOptimizeOptionL = innerConfig.Option("--noOptimize", "Do not optimize output", CommandOptionType.NoValue);
                    var floatOptionL = innerConfig.Option("--float", "Read floating point numbers as float/double instead of decimal (faster but less accurate to original)", CommandOptionType.NoValue);

                    innerConfig.OnExecute(() =>
                    {
                        // Not the proper way to show an argument problem, but this'll do for now
                        if ((filterHeaderOption.HasValue() || filterBodyOption.HasValue()) && !keyOptionL.HasValue())
                            throw new ArgumentException("Header or body filtering requires key set.");
                        IPsbFilter filter = null;
                        if (keyOptionL.HasValue()) filter = new EmoteCryptFilter(keyOptionL.ParsedValue);
                        PsbFlags flags = PsbFlags.None;
                        if (filterHeaderOption.HasValue()) flags |= PsbFlags.HeaderFiltered;
                        if (filterBodyOption.HasValue()) flags |= PsbFlags.BodyFiltered;
                        ushort version = versionOption.HasValue() ? versionOption.ParsedValue : (ushort)4;
                        SerializePsb(pathArgL.Value, version, flags, filter, !noOptimizeOptionL.HasValue(), floatOptionL.HasValue());
                    });

                    innerConfig.HelpOption();
                });

                //var pathArg = config.Argument("path", "Path of the .psb or .json file").IsRequired();
                //pathArg.Accepts().ExistingFile();
                //var keyOption = config.Option<uint>("--key", "Seed for Emote encryption filter", CommandOptionType.SingleValue);
                //var debugOption = config.Option("--debug", "Write debug files", CommandOptionType.NoValue);
                //var noOptimizeOption = config.Option("--noOptimize", "Do not optimize output", CommandOptionType.NoValue);
                //var floatOption = config.Option("--float", "Write floating point numbers as float/double instead of decimal", CommandOptionType.NoValue);

                //config.OnExecute(() =>
                //{
                //    IPsbFilter filter = null;
                //    if (keyOption.HasValue()) filter = new EmoteCryptFilter(keyOption.ParsedValue);
                //    if (Path.GetExtension(pathArg.Value).ToLower() == ".psb")
                //    {
                //        DumpPsb(pathArg.Value, debugOption.HasValue(), filter);
                //    }
                //    else
                //    {
                //        SerializePsb(pathArg.Value, filter, !noOptimizeOption.HasValue(), floatOption.HasValue());
                //    }
                //});

                config.HelpOption();

                config.OnExecute(() =>
                {
                    config.ShowHelp();
                });
            });

            app.Command("archive", (config) =>
            {
                config.FullName = "Manipulate archive";
                config.Description = "Create/extract alldata.bin files";

                config.Command("extract", (innerConfig) =>
                {
                    innerConfig.FullName = "Extract archive";
                    innerConfig.Description = "Extracts archive to folder";
                    var pathArg = innerConfig.Argument("path", "The path of the archive to extract (.bin, .psb, .psb.m)").IsRequired();
                    pathArg.Accepts().ExistingFile();
                    var outputArg = innerConfig.Argument("outputPath", "The path to extract files to");
                    var codecOpt = innerConfig.Option("--codec", "The codec used for packing/unpacking", CommandOptionType.SingleValue);
                    codecOpt.Accepts().Values(AVAILABLE_CODECS);
                    var keyOpt = innerConfig.Option("--seed", "The static seed", CommandOptionType.SingleValue);
                    var keyLengthOpt = innerConfig.Option<int>("--keyLength", "The key cycle length", CommandOptionType.SingleValue);
                    var psbKeyOption = innerConfig.Option<uint>("--key", "Seed for Emote encryption filter", CommandOptionType.SingleValue);

                    innerConfig.OnExecute(() =>
                    {
                        string archPath = pathArg.Value;
                        string extractPath = outputArg.Value ?? archPath + "_extracted";
                        MArchivePacker packer = null;
                        if (codecOpt.HasValue() && keyOpt.HasValue() && keyLengthOpt.HasValue())
                            packer = new MArchivePacker(GetCodec(codecOpt.Value()), keyOpt.Value(), keyLengthOpt.ParsedValue);
                        IPsbFilter filter = null;
                        if (psbKeyOption.HasValue()) filter = new EmoteCryptFilter(psbKeyOption.ParsedValue);
                        AllDataPacker.UnpackFiles(archPath, extractPath, packer, filter);
                    });

                    innerConfig.HelpOption();
                });

                config.Command("build", (innerConfig) =>
                {
                    innerConfig.FullName = "Build archive";
                    innerConfig.Description = "Builds archive from folder";
                    var pathArg = innerConfig.Argument("path", "The path of the folder to pack").IsRequired();
                    pathArg.Accepts().ExistingDirectory();
                    var outputArg = innerConfig.Argument("outputPath", "The path to save to; do not include file extension");
                    var codecOpt = innerConfig.Option("--codec", "The codec used for packing/unpacking", CommandOptionType.SingleValue);
                    codecOpt.Accepts().Values(AVAILABLE_CODECS);
                    var keyOpt = innerConfig.Option("--seed", "The static seed", CommandOptionType.SingleValue);
                    var keyLengthOpt = innerConfig.Option<int>("--keyLength", "The key cycle length", CommandOptionType.SingleValue);
                    var psbKeyOption = innerConfig.Option<uint>("--key", "Seed for Emote encryption filter", CommandOptionType.SingleValue);

                    innerConfig.OnExecute(() =>
                    {
                        string folderPath = pathArg.Value;
                        string outPath = outputArg.Value ?? folderPath;
                        MArchivePacker maPacker = null;
                        if (codecOpt.HasValue() && keyOpt.HasValue() && keyLengthOpt.HasValue())
                            maPacker = new MArchivePacker(GetCodec(codecOpt.Value()), keyOpt.Value(), keyLengthOpt.ParsedValue);
                        IPsbFilter filter = null;
                        if (psbKeyOption.HasValue()) filter = new EmoteCryptFilter(psbKeyOption.ParsedValue);
                        AllDataPacker.Build(folderPath, outPath, maPacker, filter);
                    });

                    innerConfig.HelpOption();
                });

                config.HelpOption();

                config.OnExecute(() =>
                {
                    config.ShowHelp();
                });
            });

            app.Command("fullunpack", (config) =>
            {
                config.FullName = "Extract and deserialize";
                config.Description = "Extracts alldata.bin and unpacks/deserializes all .psb files contained";
                var pathArg = config.Argument("path", "The path of the archive to extract (.bin, .psb, .psb.m)").IsRequired();
                var codecArg = config.Argument("codec", "The codec used for packing/unpacking").IsRequired();
                codecArg.Accepts().Values(AVAILABLE_CODECS);
                var mKeyArg = config.Argument("seed", "The static seed").IsRequired(allowEmptyStrings: true);
                var mKeyLengthArg = config.Argument<int>("keyLength", "The key cycle length").IsRequired();
                var keepOption = config.Option("--keep", "Keep packed file", CommandOptionType.NoValue);
                var psbKeyOption = config.Option<uint>("--key", "Seed for Emote encryption filter", CommandOptionType.SingleValue);
                var debugOption = config.Option("--debug", "Write debug files", CommandOptionType.NoValue);
                var outputOption = config.Option("--outputPath", "The path to extract files to", CommandOptionType.SingleValue);

                config.OnExecute(() =>
                {
                    string archPath = pathArg.Value;
                    string extractPath = outputOption.HasValue() ? outputOption.Value() : archPath + "_extracted";
                    var packer = new MArchivePacker(GetCodec(codecArg.Value), mKeyArg.Value, mKeyLengthArg.ParsedValue);
                    AllDataPacker.UnpackFiles(archPath, extractPath, packer);
                    packer.DecompressDirectory(extractPath);

                    IPsbFilter filter = null;
                    if (psbKeyOption.HasValue()) filter = new EmoteCryptFilter(psbKeyOption.ParsedValue);
                    foreach (string file in Directory.GetFiles(extractPath, "*.psb", SearchOption.AllDirectories))
                    {
                        // IIRC there was some kind of bug with globbing, so double check extension
                        if (Path.GetExtension(file).ToLower() != ".psb") continue;
                        Console.WriteLine($"Deserializing {file}");
                        DumpPsb(file, debugOption.HasValue(), filter);
                    }
                });

                config.HelpOption();
            });

            app.VersionOptionFromAssemblyAttributes(System.Reflection.Assembly.GetExecutingAssembly());
            app.HelpOption();

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 1;
            });

            try
            {
                return app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error while processing: {0}", ex);
                return -1;
            }

            // Leftover code from testing serializing/deserializing
            // Don't really want it as a part of the CLI

            // string srcPath = "";
            // string testPsbPath = "";
            // using (StreamReader sr = File.OpenText(srcPath))
            // using (StreamWriter dsw = File.CreateText(testPsbPath + ".debug.txt"))
            // using (StreamWriter jsw = File.CreateText(testPsbPath + ".json"))
            // {
            //     var root = Newtonsoft.Json.Linq.JToken.Load(new JsonTextReader(sr) { FloatParseHandling = FloatParseHandling.Decimal });
            //     var streamSource = new CliStreamSource(Path.ChangeExtension(srcPath, null) + ".streams");
            //     bool equal = Analysis.TestSerializeDeserialize(root, streamSource, testPsbPath, jsw, dsw);
            //     Console.WriteLine(equal ? "same" : "not same");
            // }
        }

        static IMArchiveCodec GetCodec(string name)
        {
            switch (name)
            {
                case "zlib":
                    return new ZlibCodec();
                case "zstd":
                case "zstandard":
                    return new ZStandardCodec();
                default:
                    throw new ArgumentException("Unknown codec name", nameof(name));
            }
        }

        static void SerializePsb(string path, ushort version, PsbFlags flags, IPsbFilter filter, bool optimize, bool readAsFloat)
        {
            string psbPath = Path.ChangeExtension(path, null);
            if (!psbPath.ToLower().EndsWith(".psb")) psbPath += ".psb";
            using (StreamReader reader = File.OpenText(path))
            using (Stream writer = File.Create(psbPath))
            {
                JsonTextReader jReader = new JsonTextReader(reader)
                {
                    FloatParseHandling = readAsFloat ? FloatParseHandling.Double : FloatParseHandling.Decimal
                };
                JToken root = JToken.ReadFrom(jReader);
                IPsbStreamSource streamSource = new CliStreamSource(Path.ChangeExtension(path, ".streams"));
                PsbWriter psbWriter = new PsbWriter(root, streamSource);
                psbWriter.Version = version;
                psbWriter.Flags = flags;
                psbWriter.Optimize = optimize;
                psbWriter.Write(writer, filter);
            }
        }

        static void DumpPsb(string psbPath, bool writeDebug, IPsbFilter filter = null)
        {
            using (FileStream fs = File.OpenRead(psbPath))
            using (StreamWriter debugWriter = writeDebug ? File.CreateText(psbPath + ".debug.txt") : null)
            using (PsbReader psbReader = new PsbReader(fs, filter, debugWriter))
            {
                JToken root;
                try
                {
                    root = psbReader.Root;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing PSB");
                    Console.WriteLine("Current position: 0x{0:x8}", fs.Position);
                    Console.WriteLine(ex);

                    if (writeDebug && filter != null)
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
#if DEBUG
                if (writeDebug)
                {
                    using (StreamWriter sw = File.CreateText(psbPath + ".keys.gv"))
                    {
                        Analysis.GenerateNameGraphDot(sw, psbReader);
                    }
                    using (StreamWriter sw = File.CreateText(psbPath + ".keyranges.txt"))
                    {
                        Analysis.GenerateNameRanges(sw, psbReader);
                    }
                    using (StreamWriter sw = File.CreateText(psbPath + ".rangevis.txt"))
                    {
                        Analysis.GenerateRangeUsageVisualization(sw, psbReader);
                    }
                    using (StreamWriter sw = File.CreateText(psbPath + ".keygen.txt"))
                    {
                        Analysis.TestKeyNamesGeneration(sw, psbReader);
                    }
                }
#endif
            }
        }
    }
}
