using TickLUA.Compilers.LUA.Lexer;

namespace TickLUA.Compilers.LUA.Parser
{
    internal abstract class AstNode
    {
        public SourceRange SourceRange { get; protected set; }

        protected static void AssertToken(Token token, params TokenType[] expected)
        {
            foreach (TokenType type in expected)
            {
                if (token.Type == type) return;
            }

            throw new CompilationException($"Unexpected symbol near '{token.Content}'", token.Position);
        }

        protected static void AssertTokenNext(LuaLexer lexer, params TokenType[] expected)
        {
            AssertToken(lexer.Current, expected);
            lexer.Next();
        }
    }
}
