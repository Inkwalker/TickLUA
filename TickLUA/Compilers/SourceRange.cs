namespace TickLUA.Compilers
{
    internal struct SourceRange
    {
        public SourcePosition from;
        public SourcePosition to;

        public SourceRange(SourcePosition from, SourcePosition to)
        {
            this.from = from;
            this.to = to;
        }
    }
}
