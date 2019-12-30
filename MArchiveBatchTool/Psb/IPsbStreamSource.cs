using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Represents a stream source.
    /// </summary>
    /// <remarks>
    /// This is used when serializing a <see cref="Newtonsoft.Json.Linq.JToken"/> to PSB, where
    /// <see cref="JStream"/>s are represented as strings rather than an actual instance. This
    /// class provides the actual data backing the stream.
    /// </remarks>
    public interface IPsbStreamSource
    {
        /// <summary>
        /// Gets the backing stream for a given <paramref name="identifier"/>.
        /// </summary>
        /// <param name="identifier">The string representation of a <see cref="JStream"/>.</param>
        /// <returns>The backing stream.</returns>
        /// <remarks>
        /// <paramref name="identifier"/> is in the form of "_type:id", where "type" is either
        /// "stream" or "bstream", and "id" is its index within the PSB.
        /// </remarks>
        Stream GetStream(string identifier);
    }
}
