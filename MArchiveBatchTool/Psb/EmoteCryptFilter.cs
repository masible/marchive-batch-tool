using System;
using System.Collections.Generic;
using System.Text;

namespace GMWare.M2.Psb
{
    /// <summary>
    /// Default <see cref="IPsbFilter"/> implementation for E-mote motion files.
    /// </summary>
    public class EmoteCryptFilter : IPsbFilter
    {
        XorShift128 rand;
        uint buffer;
        int bytesLeft;

        /// <summary>
        /// Instantiates a new instance of <see cref="EmoteCryptFilter"/>.
        /// </summary>
        /// <param name="seed">The seed used to initialize the RNG.</param>
        public EmoteCryptFilter(uint seed)
        {
            rand = new XorShift128(seed);
        }

        /// <inheritdoc/>
        public void Filter(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (buffer == 0) // M2 bug: they're checking buffer instead of bytes left
                {
                    buffer = rand.Next();
                    bytesLeft = sizeof(uint);
                }

                data[i] ^= (byte)buffer;
                buffer >>= 8;
                --bytesLeft;
            }
        }
    }
}
