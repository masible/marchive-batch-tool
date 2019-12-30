using System;
using System.Collections.Generic;
using System.Text;

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Represents a PSB data filter.
    /// </summary>
    /// <remarks>
    /// A filter can be used to encrypt or obfuscate PSB header fields and main data.
    /// </remarks>
    public interface IPsbFilter
    {
        /// <summary>
        /// Applies filtering to <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The data to apply filtering to.</param>
        void Filter(byte[] data);
    }
}
