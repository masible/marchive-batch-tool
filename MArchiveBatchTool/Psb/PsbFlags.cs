using System;
using System.Collections.Generic;
using System.Text;

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Represents PSB header flags.
    /// </summary>
    [Flags]
    public enum PsbFlags
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Header is filtered.
        /// </summary>
        HeaderFiltered = 1 << 0,
        /// <summary>
        /// Body (key names, strings, token tree) is filtered.
        /// </summary>
        BodyFiltered = 1 << 1,
    }
}
