namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal class NameRecoginzer : TokenRecognizer
    {
        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            int start_pos = source.StrPosition;
            int line = source.CursorPosition.line;
            int column = source.CursorPosition.column;

            char c = source.Peek();

            if (IsValidFirstCharacter(c))
            {
                while (IsLetterOrDigit(c) && !source.EoF)
                {
                    c = source.Next();
                }
            }

            if (source.StrPosition > start_pos)
            {
                token = new Token(TokenType.Name, line, column) { Content = source.Substring(start_pos) };
                return true;
            }

            return false;
        }

        private static bool IsValidFirstCharacter(char c)
        {
            return char.IsLetter(c) || c == '_';
        }
    }
}
