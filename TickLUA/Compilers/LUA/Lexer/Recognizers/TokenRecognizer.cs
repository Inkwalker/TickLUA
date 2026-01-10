namespace TickLUA.Compilers.LUA.Lexer.Recognizers
{
    internal abstract class TokenRecognizer
    {
        /// <summary>
        /// Try to read token from the cursor position in source code
        /// </summary>
        /// <param name="token">Token that has been read. Can be null</param>
        /// <returns>True if a token was successfully read</returns>
        public abstract bool Read(SourceCode source, out Token token);

        protected static bool IsLetterOrDigit(char c)
        {
            return char.IsDigit(c) || char.IsLetter(c) || c == '_';
        }

        protected static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        protected static bool HasRemainingCharacters(SourceCode source, int count)
        {
            if (count <= 0)
            {
                return true;
            }
            return source.Position + count <= source.Length;
        }
    }
}
