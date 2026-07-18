using System;

namespace TickLUA.VM.Serialization
{
    /// <summary>
    /// Thrown when a binary stream cannot be read back as TickLUA data:
    /// wrong magic, an incompatible compiler version, an unknown value tag,
    /// or a truncated/corrupted payload. Also thrown by <see cref="TickVM"/>
    /// when handed bytecode whose compiler version it does not support.
    /// </summary>
    public class BytecodeFormatException : Exception
    {
        public BytecodeFormatException(string message) : base(message)
        {
        }

        public BytecodeFormatException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
