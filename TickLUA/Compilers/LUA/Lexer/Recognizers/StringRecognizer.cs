namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal class StringRecognizer : TokenRecognizer
    {
        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            int start_pos = source.Position;
            int line = source.Line;
            int colon = source.Colon;

            char c = source.Peek();
            char startChar = c;

            if (startChar == '\'' || startChar == '\"')
            {
                c = source.Next();

                while (c != startChar && !source.EoF)
                {
                    c = source.Next();
                }

                if (c == startChar)
                    source.Next(); //eat closing marks
                else return false; //unclosed string
            }

            int len = source.Position - start_pos;
            if (len >= 2)
            {
                string content = source.Str.Substring(start_pos + 1, len - 2);
                content = content.Replace("\\n", "\n");

                token = new Token(TokenType.String) { Content = content, Line = line, Colon = colon };
                return true;
            }

            return false;
        }
    }
}
