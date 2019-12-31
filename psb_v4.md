M2 PSB File Format Specification, version 4
===========================================

Written by cyanic  
2019-12-31

This description was created from reverse engineering M2's PSB library. Most
names are derived from symbols contained in the library.

## 1. Introduction

The PSB format is a binary serialization format with an object representation
similar to that of JSON. Aside from the basic null, boolean, number, string,
array, and dictionary representations, PSB also supports inline binary blobs
called streams. The PSB format is used extensively for storage in M2's E-mote
SDK and the game console emulator frontend `m2engage`.

PSB uses custom serialization for arrays. Whenever you see a mention of an
array, it will be stored in this format. This serialization is described in
section 3.3.7.

All strings in are encoded in UTF-8.

## 2. File layout

```
+------------------+
|                  |
|      Header      |
|                  |
+------------------+
|                  |
| Key names entity |
|                  |
+------------------+
|                  |
|    Values tree   |
|                  |
+------------------+
|                  |
|  Strings entity  |
|                  |
+------------------+
|                  |
|     B-streams    |
|                  |
+------------------+
|                  |
|      Streams     |
|                  |
+------------------+
```

## 3. Components

### 3.1. Header

```c
struct psb_header
{
    char magicNumber[4]; // "PSB\0"
    unsigned short version;
    unsigned short flags;
    unsigned int ofstKeyOffsetArray;
    unsigned int ofstKeyEntity;
    unsigned int ofstStringOffsetArray;
    unsigned int ofstStringEntity;
    unsigned int ofstStreamOffsetArray;
    unsigned int ofstStreamSizeArray;
    unsigned int ofstStreamEntity;
    unsigned int ofstRootValue;
    // PSB v3
    unsigned int checksum;
    // PSB v4
    unsigned int ofstBStreamOffsetArray;
    unsigned int ofstBStreamSizeArray;
    unsigned int ofstBStreamEntity;
};
```

- `magicNumber`: The string `PSB` ending with a null character. This identifies
  the file as a PSB file.
- `version`: The version of the PSB file.
- `flags`: Flags that apply to the entire file.
  - `PSB_FLAGS_HEADER_FILTERED = (1 << 0)`: Header fields aside from magic
    number, version, and flags are filtered.
  - `PSB_FLAGS_BODY_FILTERED = (1 << 1)`: Non-header components except streams
    and B-streams are filtered.
- `ofstKeyOffsetArray`: The offset to key names offsets array. Only used for v1.
- `ofstKeyEntity`: The offset to the key names entity. For v1, the key names
  entity is a concatenation of strings. For other versions, see section about
  the key names trie.
- `ofstStringOffsetArray`: The offset to the strings offsets array.
- `ofstStringEntity`: The offset to the strings entity. The strings entity is
  a concatenation of strings.
- `ofstStreamOffsetArray`: The offset to the streams offsets array.
- `ofstStreamSizeArray`: The offset to the streams sizes array.
- `ofstStreamEntity`: The offset to the streams entity.
- `ofstRootValue`: The offset to the root node of the PSB.
- `checksum`: The Adler-32 checksum of the unfiltered header, excluding this
  field entirely in the calculation.
- `ofstStreamOffsetArray`: The offset to the B-streams offsets array.
- `ofstStreamSizeArray`: The offset to the B-streams sizes array.
- `ofstStreamEntity`: The offset to the B-streams entity.

### 3.2. Key names entity

#### 3.2.1. Array form

In PSB v1, key names are stored as an array of offsets and a buffer of
concatenated strings. The values are sorted by ordinal. To look up a key name,
index into the offsets array, and using the value, index into the entity char
buffer and read as a UTF-8 encoded C string.

#### 3.2.2. Trie form

In PSB v2 and above, the key names are stored as a trie, with lookups occurring
from value to key. The entity is made of three arrays:

- `base`: Value to subtract from child node indexes to obtain the node's value.
  If a tail node, the index of the associated key name that can be obtained from
  following this node towards the root.
- `check`: Index of the parent node to the current node.
- `tail`: Index to the tail node of the associated key name.

---

A trie node can be represented as such:

```c
struct trie_node
{
    unsigned int index;
    unsigned int parent;
    unsigned int node_base;
    unsigned char value;
    unsigned int children[];
};
```

`parent` corresponds to `check[index]`, and `node_base` corresponds to
`base[index]`.

Each node represents one byte in a key name. Because strings are encoded in
UTF-8, this means multi-byte characters are split up into separate nodes.

To construct a trie node, its child nodes are sorted by value, and the minimum
and maximum values are found. Child nodes include tail nodes, which have a value
of `0` (i.e. null character). A free index range is found that will accomodate
the indexes of each child node. The child nodes have their indexes assigned
from this range, and `node_base` is set such that subtracting it from a child
node's index will result in that node's value. For tail nodes, the `node_base`
is the index of the string constructed by traversal from the root node to this
node. Each child node has its `parent` value set to the current node's `index`.

The root node occupies index `0`. Because `node_base` is unsigned and the root
node is occupied, it is at least `1`, hence nodes with a depth of 1 have indexes
that is 1 greater than its value.

To reconstruct a key name by its index, index the `tail` array with the key's
index, and follow the nodes from the tail node to the root node, collecting
values along the way. A node's value can be calculated as `index -
base[check[index]]`. Reverse the order of the values collected when the root
node is reached, and decode as a UTF-8 C string.

As an example, consider a trie containing the strings `AC`, `DC`, and
`DCE`. First level values are 0x41 and 0x44, and given indexes `66` and `69`
respectively. Off of `A` is the value `C`. To ensure its parent has a
`node_base` of at least `1`, it is assigned index `68.` Following that, the
string terminator is given an index of `1` and its parent assigned a
`node_base` of `1`. Its own `node_base` is `0` because it is the first string.
For strings `DC` and `DCE`, `D` only has the child `C`. To comply with the
`node_base` requirement, its minimum index value is 68, but because the index
has already been occupied by the `C` from `AC` and index 69 is occupied by
`D`, it is instead assigned index 70, and its parent the `node_base` of `3`.
Following this is the null terminator and `E`, for a range from 0x00 to 0x45.
The lowest node index available is `2`, and based on this, the index of `E` is
`71`. Since neither indexes are used, they are assigned to the nodes. The
parent node gains a `node_base` of `2`. The null terminator of `DC` is
assigned a `node_base` of `1`. Finally, for the null terminator of `DCE`, it
is assigned the lowest available index of `3` and `node_base` of `2`. There is
a gap from index `4` to `65` and a gap at index `67`; the `base` and `check`
values for these are filled with `0`.

For the tail array, its values are `1`, `2`, and `3`, pointing to the null
terminators at the end of each key nams.

The finished trie can be seen in the following diagram:

```
index   0  1  2  3  4 ... 65 66 67 68 69 70 71
value     \0 \0 \0            A     C  D  C  E
base    1  0  1  2  0      0  1  0  1  3  2  3
check   0 68 70 71  0      0  0  0 66  0 69 70

index   0  1  2
tail    1  2  3
```

This trie has `base` and `check` array lengths of `72` and `tail` array length
of `3`.

### 3.3. Values tree

The values tree contains the primary structure of the PSB file. It starts with
the root token. Each token consists of a 1-byte type identifier, followed by
its encoded value. The following table lists the types each identifier value
corresponds to.

| Value | Type                                       |
|:-----:|--------------------------------------------|
|   0   | Invalid                                    |
|   1   | `null`                                     |
|   2   | `true`                                     |
|   3   | `false`                                    |
|   4   | 32-bit signed integer, value `0`           |
|   5   | 32-bit signed integer, stored in 8 bits    |
|   6   | 32-bit signed integer, stored in 16 bits   |
|   7   | 32-bit signed integer, stored in 24 bits   |
|   8   | 32-bit signed integer, stored in 32 bits   |
|   9   | 64-bit signed integer, stored in 40 bits   |
|   10  | 64-bit signed integer, stored in 48 bits   |
|   11  | 64-bit signed integer, stored in 56 bits   |
|   12  | 64-bit signed integer, stored in 64 bits   |
|   13  | 32-bit unsigned integer, stored in 8 bits  |
|   14  | 32-bit unsigned integer, stored in 16 bits |
|   15  | 32-bit unsigned integer, stored in 24 bits |
|   16  | 32-bit unsigned integer, stored in 32 bits |
|   17  | Key name index, stored in 8 bits           |
|   18  | Key name index, stored in 16 bits          |
|   19  | Key name index, stored in 24 bits          |
|   20  | Key name index, stored in 32 bits          |
|   21  | String index, stored in 8 bits             |
|   22  | String index, stored in 16 bits            |
|   23  | String index, stored in 24 bits            |
|   24  | String index, stored in 32 bits            |
|   25  | Stream index, stored in 8 bits             |
|   26  | Stream index, stored in 16 bits            |
|   27  | Stream index, stored in 24 bits            |
|   28  | Stream index, stored in 32 bits            |
|   29  | Single precision floating point, value `0` |
|   30  | Single precision floating point            |
|   31  | Double precision floating point            |
|   32  | Array                                      |
|   33  | Object                                     |
|   34  | B-stream index, stored in 8 bits           |
|   35  | B-stream index, stored in 16 bits          |
|   36  | B-stream index, stored in 24 bits          |
|   37  | B-stream index, stored in 32 bits          |

#### 3.3.1. Invalid

**Identifier value**: `0`

This type should never occur in a valid tree.

#### 3.3.2. `null`

**Identifier value**: `1`

This represents the value `null`.

#### 3.3.3. Boolean values

**Identifier values**: `2`, `3`

This represents the boolean values `true` and `false`, respectively.

#### 3.3.4. 32-bit signed integer

**Identifier values**: `4`, `5`, `6`, `7`, `8`

This type represents a 32-bit signed integer. Identifier value `4` represents
the value `0`. The other identifiers indicate that the integer value follows
in the next 1 to 4 bytes, respectively. The bytes are to be interpreted as
little-endian two's complement integers of the appropriate number of bits, and
extended to 32 bits as necessary.

#### 3.3.5. 64-bit signed integer

**Identifier values**: `9`, `10`, `11`, `12`

This type represents a 64-bit signed integer. The identifiers indicate that the
integer value follows in the next 5 to 8 bytes, respectively. The bytes are to
be interpreted as little-endian two's complement integers of the appropriate
number of bits, extended to 64 bits as necessary. A value of `0` should be
encoded with its 32-bit signed integer representation.

#### 3.3.6. 32-bit unsigned integer

**Identifier values**: `13`, `14`, `15`, `16`

This type represents a 32-bit unsigned integer. It is generally used in
conjunction with uint arrays. The identifiers indicate that the integer value
follows in the next 1 to 4 bytes, respectively. The bytes are to be interpreted
as little-endian unsigned integers of the appropriate number of bits, extended
to 32-bits as nucessary.

#### 3.3.7. Pseudo type: unsigned integer array

This type does not have an identifier value, but is used as part of interpreting
other types. It is composed of an unsigned integer token that represents the
number of elements in the array, a single type identifier that indicates the
size of each value in the array, and the actual array values.

For example, the array `[0, 1, 2, 3, 4]` would be stored as follows:

```c
unsigned char uintArrayRep[] = {
    13, 5, // Count: identifier value 13 for 8-bit value, then value 5
    13, // Size of values: identifier value 13 for 8-bit values
    0, 1, 2, 3, 4 // Values in array, each 8-bits as indicated by size of values
};
```

#### 3.3.8. Key name index

**Identifier values**: `17`, `18`, `19`, `20`

This type represents a key name index. It should be interpreted similarly to
a 32-bit unsigned integer token. This token type only occurs in PSB v1 when
reading objects.

#### 3.3.9. String index

**Identifier values**: `21`, `22`, `23`, `24`

This type represents string index. It should be interpreted similarly to a
32-bit unsigned integer token.

#### 3.3.10. Stream index

**Identifier values**: `25`, `26`, `27`, `28`

This type represents a stream index. It should be interpreted similarly to a
32-bit unsigned integer token.

#### 3.3.11. Single precision floating point

**Identifier values**: `29`, `30`

This type represents a single precision floating point number. Identifier
value `29` indicates the value `0.0`, while identifier value `30` indicates
that an IEEE 754 single precision floating point value follows.

#### 3.3.12. Double precision floating point

**Identifier value**: `31`

This type represents a double precision floating point number. The identifier
value indicates that an IEEE 754 double precision floating point value follows.
A value of `0.0` should be encoded with its single precision floating point
representation.

#### 3.3.13. Array

**Identifier value**: `32`

This type represents an array of tokens. It is composed of an offsets uint array
and a concatenated collection of the array values' tokens. To retrieve an array
element, index into the offsets array, seek to the offset relative to the
beginning of the concatenated collection of token data, and read the token at
that offset.

#### 3.3.14. Object

**Identifier value**: `33`

This type represends a collection of key-value pairs. How it is stored depends
on the PSB version.

All values are sorted by their keys by ordinal.

##### 3.3.14.1. PSB v1

The value is composed of an array of offsets, and for each key-value pair,
a key token and the corresponding value token. To read a key-value pair,
index into the offsets array, and seek to the offset relative to the beginning
of the collection of the key-value tokens. Read the key token (see section
3.3.8), then read the value token.

##### 3.3.14.2. PSB v2 and above

The value is composed of an array of key name indexes, an array of value token
offsets, and a collection of value tokens. The advantage of this layout is so
key names can be loaded in one chunk instead of having to seek to each
key-value pair. To load a value, look up the index of the key through the
array of key names, then index into the offsets array. Seek to the offset
relative to the beginning of the value tokens collection, and read the token.

#### 3.3.15. B-stream index

**Identifier values**: `34`, `35`, `36`, `37`

This type represents a B-stream index. It should be interpreted similarly to a
stream index, but the value reflects an index into B-stream storage rather than
stream storage. This type is only present in PSB v4 and above.

### 3.4. Strings entity

Strings are stored similar to key names in PSB v1. It consists of an array of
offsets, and a buffer of concatenated strings. The strings are sorted by
ordinal. See section 3.2.1 for further details.

### 3.5. Streams

Streams are composed of three parts: the offsets array, the sizes array, and
the entity which is all the streams concatenated with padding. Stream data are
aligned with padding in front of each stream data. The alignment size is
platform-dependent, and padding consists of null bytes. To read a stream,
index into the offsets array, and seek to the offset relative to the start of
the streams entity. The size of the data can be obtained from indexing into the
sizes array.

### 3.6. B-Streams

Starting with PSB v4, B-stream is a storage designation for streams separate
from the normal streams. Its purpose is not clearly known, but its data is not
discarded when a PSB loaded in memory is purged. It is stored in the same way
as normal streams (see section 3.5).

## 4. Optimization

Arrays, objects, key names, strings, and streams can be optimized, meaning that
duplication of values could be reduced.

In arrays and objects, optimization can only be applied to its immediate
children due to the offset being relative to its child values collection. To
apply optimization, compare the value of a token that is being inserted with
the tokens already present, and if it is equal to any previous token, write
the offset of the existing token instead of writing a new offset and inserting
the new token.

For key names and strings, strings being inserted should be checked against
existing strings, and discarded if already present. When writing indexes, look
up the index of the string in the array of sorted strings.

Streams are optimized in the same way as key names and strings.

## 5. Filtering

PSB files can be filtered to encrypt the header and non-stream components. When
`PSB_FLAGS_HEADER_FILTERED` is set in the header flags, all header fields except
the magic value, version, and flags are filtered. When `PSB_FLAGS_BODY_FILTERED`
is set, all data between the header and the start of the streams offsets array
(B-streams offsets array for PSB v4) are filtered. The same filter is used to
filter the header first, then the body.

The default filter implementation is based on XorShift128, where a seed is
used to initialize the RNG, and the random output of the RNG is applied one byte
at a time, least significant byte first.
