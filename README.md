# GMWare.M2

[![Nuget](https://img.shields.io/nuget/v/GMWare.M2)](https://www.nuget.org/packages/GMWare.M2/)

This library is designed to manipulate files in formats created by M2 Co., Ltd.
This includes the PSB format, `.m` (MArchive) format, and archive files (e.g.
`alldata.bin`). These files can be found in games using M2's E-mote SDK and
products using M2's emulation frontend `m2engage`.

## Supported formats

- **PSB**: PSB is a binary serialization of JSON-like data, used extensively in
  E-mote motion files and other resources and configuration files in `m2engage`.
  Classes for manipulating PSB files are in `GMWare.M2.Psb`. Primarily,
  `PsbReader` and `PsbWriter` are used for deserializing and serializing between
  PSB and JSON.NET's `JToken`s. PSB filtering is supported, and an
  implementation of the default filter used for E-mote is included.
- **MArchive**: These are compression/encryption wrappers with file extension
  `.m`. Classes for manipulating them are in `GMWare.M2.MArchive`. Codecs for
  `mdf` (Zlib) and `mzs` (ZStandard) are included.
- **PSB archive resource**: Commonly known as `alldata.bin`, the corresponding
  PSB file serves as a manifest while the `.bin` file itself contains all the
  data. The class for manipulating these files is
  `GMWare.M2.MArchive.AllDataPacker`.

This library does not offer specific abstractions for manipulating E-mote motion
files. If you are looking for that, try [FreeMote](https://github.com/UlyssesWu/FreeMote).

## Compatibility

The PSB serializer attempts to create files that are as close in specifications
to original PSB files created by M2's SDKs as possible. All currently known
versions of PSB files (v1 to v4) can be serialized/deserialized. Generated PSB
name tables are created with tight packing, and PSB optimization is supported.
JSON.NET is used for convenience for converting deserialized PSBs to object
instances and vice versa.

## Examples

### PSB Deserialization

```csharp
using (FileStream fs = File.OpenRead(psbPath))
using (PsbReader psbReader = new PsbReader(fs))
{
    JToken root = psbReader.Root;

    // Do what you need with root here
}
```

Accessing `PsbReader.Root` will load the content from the PSB file.

By default lazy stream loading is enabled. If you need access to streams within
the PSB but want to dispose the reader before that, make sure to call
`PsbReader.LoadAllStreamData()` before you dispose.

### PSB Serialization

```csharp
using (Stream fs = File.Create(psbPath))
{
    // root: The JToken you want to serialize
    // streamSource: The stream source, if you have JStreams that don't have
    //               their data loaded
    PsbWriter psbWriter = new PsbWriter(root, streamSource) {
		Version = 4
	};
    psbWriter.Write(fs);
}
```

Set version, flags, and whether to optimize before you call `PsbWriter.Write()`.

### MArchive

```csharp
var packer = new MArchivePacker(new ZlibCodec(), keyString, 64);
if (File.Exists(path))
{
    packer.CompressFile(path);
}
else
{
    packer.CompressDirectory(path);
}
```

The opposite functions `MArchivePacker.DecompressFile()` and
`MArchivePacker.DecompressDirectory()` work the same way. Each of these
functions take `keepOrig`. The `.m` or decompressed file will be deleted if
set to `false`. Set to `true` to keep the source file.
`MArchivePacker.CompressDirectory()` has additionally a `forceCompress`
argument. By default there's a list of folder names where the files inside
will not be compressed. Set to `true` to force compression.

### PSB Archive resource

```csharp
var packer = new MArchivePacker(new ZlibCodec(), keyString, 64);
AllDataPacker.UnpackFiles(archPath, extractPath, packer);
AllDataPacker.Build(folderPath, outPath, packer);
```

For `AllDataPacker.UnpackFiles()`, you can supply either the `.bin`, `.psb`,
or the `.psb.m` file as the input path. For `AllDataPacker.Build()`, do not
include `.bin`, `.psb`, or `.psb.m` in the output path.

Supplying a `MArchivePacker` is optional. It is used to pack the `.psb` manifest
into a `.m` file.
