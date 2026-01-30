namespace TickLUA.Compilers
{
    internal struct SourcePosition
    {
        public int line;
        public int column;

        public SourcePosition(int line, int column)
        {
            this.line = line;
            this.column = column;
        }

        public static SourcePosition operator +(SourcePosition lhs, int rhs)
        {
            return new SourcePosition(lhs.line, lhs.column + rhs);
        }

        public static SourcePosition operator -(SourcePosition lhs, int rhs)
        {
            return new SourcePosition(lhs.line, lhs.column - rhs);
        }

        public SourcePosition NewLine() => new SourcePosition(line + 1, 1);
    }
}
