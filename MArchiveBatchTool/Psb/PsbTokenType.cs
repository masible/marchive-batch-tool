using System;
using System.Collections.Generic;
using System.Text;

namespace MArchiveBatchTool.Psb
{
    public enum PsbTokenType
    {
        Invalid,
        Null,
        Bool,
        Int,
        Long,
        UIntArray,
        Key,
        String,
        Stream,
        Float,
        Double,
        TokenArray,
        Object,
        BStream
    }
}
