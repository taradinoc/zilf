using System;

namespace Zilf.ZModel.Values
{
    [Flags]
    public enum TableFlags
    {
        /// <summary>
        /// The table elements are bytes rather than words.
        /// </summary>
        Byte = 1,
        /// <summary>
        /// The table elements are 4-byte records rather than words, and
        /// the table is prefixed with an element count (byte) and a zero byte.
        /// </summary>
        Lexv = 2,
        /// <summary>
        /// The table is prefixed with an element count (byte).
        /// </summary>
        ByteLength = 4,
        /// <summary>
        /// The table is prefixed with an element count (word).
        /// </summary>
        WordLength = 8,
        /// <summary>
        /// The table is stored in pure (read-only) memory.
        /// </summary>
        Pure = 16,
    }
}