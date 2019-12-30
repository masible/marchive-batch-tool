using System;
using System.Collections.Generic;
using System.Text;

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Represents the types of tokens in a PSB.
    /// </summary>
    enum PsbTokenType
    {
        /// <summary>
        /// An unknown token type.
        /// </summary>
        Invalid,
        /// <summary>
        /// The value <see cref="Null"/>.
        /// </summary>
        Null,
        /// <summary>
        /// The values <c>true</c> and <c>false</c>.
        /// </summary>
        Bool,
        /// <summary>
        /// A signed 32-bit integer.
        /// </summary>
        Int,
        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
        Long,
        /// <summary>
        /// An unsigned 32-bit integer.
        /// </summary>
        UInt,
        /// <summary>
        /// Index into key names array (v1 only).
        /// </summary>
        Key,
        /// <summary>
        /// A string.
        /// </summary>
        String,
        /// <summary>
        /// A stream.
        /// </summary>
        Stream,
        /// <summary>
        /// A single precision floating point number.
        /// </summary>
        Float,
        /// <summary>
        /// A double precision floating point number.
        /// </summary>
        Double,
        /// <summary>
        /// An array of tokens.
        /// </summary>
        TokenArray,
        /// <summary>
        /// A dictionary of strings and tokens.
        /// </summary>
        Object,
        /// <summary>
        /// A B-stream.
        /// </summary>
        BStream
    }
}
