namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal class NameRecoginzer : TokenRecognizer
    {
        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            int start_pos = source.Position;
            int line = source.Line;
            int colon = source.Colon;

            char c = source.Peek();

            if (IsValidFirstCharacter(c))
            {
                while (IsLetterOrDigit(c) && !source.EoF)
                {
                    c = source.Next();
                }
            }

            if (source.Position > start_pos)
            {
                token = new Token(TokenType.Name) { Content = source.Substring(start_pos), Line = line, Colon = colon };
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
