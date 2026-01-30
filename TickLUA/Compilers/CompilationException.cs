using System;

namespace TickLUA.Compilers
{
    internal class CompilationException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public CompilationException(string message, int l, int ch) : base(message) 
        { 
            Line = l;
            Column = ch;
        }

        public CompilationException(string message, SourcePosition pos) : base(message)
        {
            Line = pos.line;
            Column = pos.column;
        }
    }
}
