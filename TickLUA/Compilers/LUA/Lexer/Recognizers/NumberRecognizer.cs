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
            char prev = ' ';

            // First character must be a digit (we don't support floats starting from a dot (.5))
            if (!IsDigit(c)) 
            {
                return false;
            }

            int dotCount = 0;
            int eCount = 0;
            bool isHex = false;

            while (IsValidNumberChar(c) && !source.EoF)
            {
                if (c == '.')
                {
                    dotCount++;
                    if (dotCount > 1)
                    {
                        break;
                    }
                }
                else if (IsExpChar(c, isHex))
                {
                    eCount++;
                    if (eCount > 1)
                    {
                        break;
                    }
                }
                else if (IsXChar(c))
                {
                    if ((start_pos - source.Position != 1) && prev != '0')
                        break;
                    isHex = true;
                }
                else if (IsSignChar(c))
                {
                    if (!IsExpChar(prev, isHex))
                    {
                        break;
                    }
                }

                prev = c;
                c = source.Next();
            }

            if (source.Position > start_pos)
            {
                token = new Token(TokenType.Number) { Content = source.Substring(start_pos), Line = line, Column = column };
                return true;
            }

            return false;
        }

        private static bool IsValidNumberChar(char c)
        {
            return IsDigit(c) || IsHexChar(c) || c == '.' || IsExpChar(c, true) || IsExpChar(c, false) || IsSignChar(c) || IsXChar(c);
        }

        private static bool IsExpChar(char c, bool hex)
        {
            if (hex)
                return c == 'p' || c == 'P';
            else
                return c == 'e' || c == 'E';
        }

        private static bool IsSignChar(char c)
        {
            return c == '+' || c == '-';
        }

        private static bool IsXChar(char c)
        {
            return c == 'x' || c == 'X';
        }

        private static bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }
    }
}
