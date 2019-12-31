# GMWare.M2

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
