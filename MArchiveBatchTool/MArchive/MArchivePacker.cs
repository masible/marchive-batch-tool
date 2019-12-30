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

namespace GMWare.M2.MArchive
{
    /// <summary>
    /// Provides functions for encrypting and decrypting MArchive encoded files.
    /// </summary>
    public class MArchivePacker
    {
        IMArchiveCodec codec;
        string seed;
        int keyLength;
        List<string> noCompressionFilters = new List<string>() { "sound" };

        /// <summary>
        /// Instantiates a new instance of <see cref="MArchivePacker"/>.
        /// </summary>
        /// <param name="codec">The <see cref="IMArchiveCodec"/> used for compression.</param>
        /// <param name="seed">The encryption seed.</param>
        /// <param name="keyLength">The key period.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="codec"/> or <paramref name="seed"/> is <c>null</c>.</exception>
        public MArchivePacker(IMArchiveCodec codec, string seed, int keyLength)
        {
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
            this.seed = seed ?? throw new ArgumentNullException(nameof(seed));
            this.keyLength = keyLength;
        }

        /// <summary>
        /// Gets or sets filters for files that will not have compression applied by default.
        /// </summary>
        /// <exception cref="ArgumentNullException">When attempting to set to <c>null</c>.</exception>
        /// <remarks>
        /// If a file's parent directory has a name in these filters, the file is not compressed
        /// when compressing directories. By default the list contains the "sound" directory.
        /// </remarks>
        public List<string> NoCompressionFilters
        {
            get => noCompressionFilters;
            set => noCompressionFilters = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Decompreses a file.
        /// </summary>
        /// <param name="path">The path of the file to decompress.</param>
        /// <param name="keepOrig">Whether to keep the original .m file.</param>
        /// <remarks>The decompressed file is the same name but with ".m" extension removed.</remarks>
        public void DecompressFile(string path, bool keepOrig = false)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (Path.GetExtension(path).ToLower() != ".m")
                throw new ArgumentException("File is not compressed.", nameof(path));

            using (FileStream fs = File.OpenRead(path))
            {
                BinaryReader br = new BinaryReader(fs);
                uint magic = br.ReadUInt32();
                // TODO: dynamically grab the right codec
                if (magic != codec.Magic) throw new ArgumentException("Codec mismatch", nameof(path));
                int decompressedLength = br.ReadInt32();

                using (FileStream ofs = File.Create(Path.ChangeExtension(path, null)))
                using (MArchiveCryptoStream cs = new MArchiveCryptoStream(fs, path, seed, keyLength))
                using (Stream decompStream = codec.GetDecompressionStream(cs))
                {
                    decompStream.CopyTo(ofs);
                    ofs.Flush();
                    if (ofs.Length != decompressedLength)
                        throw new InvalidDataException("Decompressed stream length is not same as expected.");
                }
            }

            if (!keepOrig) File.Delete(path);
        }

        /// <summary>
        /// Compresses a file.
        /// </summary>
        /// <param name="path">The path of the file to compress.</param>
        /// <param name="keepOrig">Whether to keep the uncompressed file.</param>
        /// <remarks>The compressed file will have ".m" extension appended.</remarks>
        public void CompressFile(string path, bool keepOrig = false)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string destPath = path + ".m";
            using (FileStream fs = File.OpenRead(path))
            using (FileStream ofs = File.Create(destPath))
            {
                BinaryWriter bw = new BinaryWriter(ofs);
                bw.Write(codec.Magic);
                bw.Write((int)fs.Length);

                using (MArchiveCryptoStream cs = new MArchiveCryptoStream(ofs, destPath, seed, keyLength))
                using (Stream compStream = codec.GetCompressionStream(cs))
                {
                    fs.CopyTo(compStream);
                }
            }

            if (!keepOrig) File.Delete(path);
        }

        /// <summary>
        /// Compresses a directory recursively.
        /// </summary>
        /// <param name="path">The path to the directory to compress.</param>
        /// <param name="keepOrig">Whether to keep the uncompressed files.</param>
        /// <param name="forceCompress">Whether to ignore no compression filters.</param>
        /// <remarks>
        /// This is equivalent to running <see cref="CompressFile(string, bool)"/> on each
        /// file in <paramref name="path"/> except for files that match <see cref="NoCompressionFilters"/>.
        /// </remarks>
        public void CompressDirectory(string path, bool keepOrig = false, bool forceCompress = false)
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).ToLower() == ".m") continue;
                string containingDir = Path.GetFileName(Path.GetDirectoryName(file));
                if (forceCompress || !noCompressionFilters.Contains(containingDir))
                {
                    Console.WriteLine($"Compressing {file}");
                    CompressFile(file, keepOrig);
                }
            }
        }

        /// <summary>
        /// Decompresses a directory recursively.
        /// </summary>
        /// <param name="path">The path to the directory to decompress.</param>
        /// <param name="keepOrig">Whether to keep the .m files.</param>
        /// <remarks>
        /// This is equivalent to running <see cref="DecompressFile(string, bool)"/> on each
        /// file in <paramref name="path"/>.
        /// </remarks>
        public void DecompressDirectory(string path, bool keepOrig = false)
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).ToLower() == ".m")
                {
                    Console.WriteLine($"Decompressing {file}");
                    DecompressFile(file, keepOrig);
                }
            }
        }
    }
}
