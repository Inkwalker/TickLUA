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
            char startChar = c;

            if (startChar == '-')
            {
                c = source.Next();

                if (c != '-') return false;

                while (c != '\n' && !source.EoF)
                {
                    c = source.Next();
                }

                return true;
            }

            return false;
        }
    }
}
