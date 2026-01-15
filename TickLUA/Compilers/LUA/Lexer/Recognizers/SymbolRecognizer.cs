namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal class SymbolRecognizer : TokenRecognizer
    {
        private string literal;
        private TokenType tokenType;

        public SymbolRecognizer(string literal, TokenType tokenType)
        {
            this.literal = literal;
            this.tokenType = tokenType;
        }

        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            if (!HasRemainingCharacters(source, literal.Length)) return false;

            int start_position = source.Position;
            int line = source.Line;
            int column = source.Column;

            char c = source.Peek();
            for (int i = 0; i < literal.Length; i++)
            {
                if (c != literal[i]) return false;
                c = source.Next();
            }

            token = new Token(tokenType) { Content = source.Substring(start_position), Line = line, Column = column };

            return true;
        }
    }
}
