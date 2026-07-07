namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    /// <summary>
    /// Consumes comments from source code during tokenization.
    /// </summary>
    internal class CommentRecognizer : TokenRecognizer
    {
        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            char c = source.Peek();

            if (c != '-') return false;

            c = source.Next();

            if (c != '-') return false;

            // We have "--". Look at the character that follows.
            c = source.Next();

            // A long-bracket "[[" turns this into a block comment.
            if (c == '[')
            {
                c = source.Next();

                if (c == '[')
                {
                    ReadBlockComment(source);
                    return true;
                }
            }

            // Otherwise it is a line comment: consume until end of line.
            while (c != '\n' && !source.EoF)
            {
                c = source.Next();
            }

            return true;
        }

        private static void ReadBlockComment(SourceCode source)
        {
            char c = source.Next();

            while (!source.EoF)
            {
                if (c == ']')
                {
                    c = source.Next();

                    if (source.EoF) return;

                    if (c == ']')
                    {
                        source.Next(); // eat the closing "]]"
                        return;
                    }

                    continue;
                }

                c = source.Next();
            }
        }
    }
}
