namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    /// <summary>
    /// Consumes whitespace and semicolon characters from source code during tokenization.
    /// </summary>
    internal class WhiteSpaceRecognizer : TokenRecognizer
    {
        public override bool Read(SourceCode source, out Token token)
        {
            token = null;

            bool success = false;
            char c = source.Peek();
            while (!source.EoF && IsWhiteSpaceCharacter(c))
            {
                c = source.Next();
                success = true;
            }

            return success;
        }

        private bool IsWhiteSpaceCharacter(char character) => char.IsWhiteSpace(character) || character == ';';
    }
}
