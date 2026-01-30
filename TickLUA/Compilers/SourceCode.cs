using System.Collections.Generic;

namespace TickLUA.Compilers
{
    internal class SourceCode
    {
        public string Str { get; }

        public int StrPosition { get; private set; }
        public SourcePosition CursorPosition { get; private set; }

        public bool EoF => StrPosition >= Str.Length;
        public int Length => Str.Length;

        private List<int> line_length = new List<int>();

        public SourceCode(string str)
        {
            Str = str;
            StrPosition = 0;
            CursorPosition = new SourcePosition(1, 1);

            line_length.Add(0);
        }

        /// <summary>
        /// Move cursor back to character position <paramref name="pos"/> 
        /// while keeping track of lines. 
        /// </summary>
        /// <param name="pos"></param>
        public void Revert(int pos)
        {
            while (StrPosition > pos)
            {
                StrPosition--;
                CursorPosition -= 1;

                if (Str[StrPosition] == '\n')
                {
                    int l = CursorPosition.line - 1;
                    int c = line_length[l];
                    CursorPosition = new SourcePosition(l, c);
                }
            }
        }

        /// <summary>
        /// Get next character and advance cursor position
        /// </summary>
        public char Next()
        {
            if (Str[StrPosition] == '\n')
            {
                if (line_length.Count < CursorPosition.line + 1)
                    line_length.Add(CursorPosition.column);
                else
                    line_length[CursorPosition.column - 1] = CursorPosition.column;

                CursorPosition = CursorPosition.NewLine();
            }
            else
            {
                CursorPosition += 1;
            }
            StrPosition++;
            return EoF ? ' ' : Str[StrPosition];
        }

        /// <summary>
        /// Get next character without advancing cursor position
        /// </summary>
        public char Peek() => EoF ? ' ' : Str[StrPosition];

        /// <summary>
        /// Get a substring starting from <paramref name="start"/> to current cursor position
        /// </summary>
        public string Substring(int start) => Str.Substring(start, StrPosition - start);
    }
}
