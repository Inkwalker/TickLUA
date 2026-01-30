namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal class KeywordRecognizer : TokenRecognizer
    {
        private string literal;
        private TokenType tokenType;

        public KeywordRecognizer(string literal, TokenType tokenType)
        {
            this.literal = literal;
            this.tokenType = tokenType;
        }

        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            if (!HasRemainingCharacters(source, literal.Length)) return false;

            //Check what's after the keyword. If it's longer than expected then it's a wrong keyword.
            if (source.StrPosition + literal.Length < source.Length)
            {
                var next_c = source.Str[source.StrPosition + literal.Length];
                if (IsLetterOrDigit(next_c)) return false;
            }

            int line = source.CursorPosition.line;
            int column = source.CursorPosition.column;
            int start_pos = source.StrPosition;

            char c = source.Peek();
            for (int i = 0; i < literal.Length; i++)
            {
                if (c != literal[i]) return false;

                c = source.Next();
            }

            token = new Token(tokenType, line, column) { Content = source.Substring(start_pos) };

            return true;
        }
    }
}
