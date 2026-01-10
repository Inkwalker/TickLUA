namespace TickLUA.Compilers
{
    internal class SourceCode
    {
        public string Str { get; }

        public int Position { get; private set; }
        public int Line { get; private set; }
        public int Colon { get; private set; }

        public bool EoF => Position >= Str.Length;
        public int Length => Str.Length;

        public SourceCode(string str)
        {
            Str = str;
            Position = 0;
            Line = 1;
            Colon = 1;
        }

        /// <summary>
        /// Move cursor back to character position <paramref name="pos"/> 
        /// while keeping track of lines. 
        /// </summary>
        /// <param name="pos"></param>
        public void Revert(int pos)
        {
            while (Position > pos)
            {
                Position--;
                Colon--;

                if (Str[Position] == '\n')
                {
                    Line--;
                    Colon = 1;
                }
            }
        }

        /// <summary>
        /// Get next character and advance cursor position
        /// </summary>
        public char Next()
        {
            if (Str[Position] == '\n')
            {
                Line++;
                Colon = 0;
            }
            Position++;
            Colon++;
            return EoF ? ' ' : Str[Position];
        }

        /// <summary>
        /// Get next character without advancing cursor position
        /// </summary>
        public char Peek() => EoF ? ' ' : Str[Position];

        /// <summary>
        /// Get a substring starting from <paramref name="start"/> to current cursor position
        /// </summary>
        public string Substring(int start) => Str.Substring(start, Position - start);
    }
}
