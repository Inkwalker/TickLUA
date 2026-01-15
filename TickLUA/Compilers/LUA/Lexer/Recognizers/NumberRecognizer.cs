namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal class NumberRecognizer : TokenRecognizer
    {
        public override bool Read(SourceCode source, out Token token)
        {
            //TODO: add support for hex notation and exponents

            token = null;

            int start_pos = source.Position;
            int line = source.Line;
            int column = source.Column;

            char c = source.Peek();
            while ((IsDigit(c) || c == '.') && !source.EoF)
            {
                c = source.Next();
            }

            if (source.Position > start_pos)
            {
                token = new Token(TokenType.Number) { Content = source.Substring(start_pos), Line = line, Column = column };
                return true;
            }

            return false;
        }
    }
}
