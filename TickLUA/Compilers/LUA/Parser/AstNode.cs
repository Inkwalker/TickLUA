using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser
{
    internal abstract class AstNode
    {
        protected static void AssertToken(Token token, params TokenType[] expected)
        {
            foreach (TokenType type in expected)
            {
                if (token.Type == type) return;
            }

            throw new CompilationException($"Unexpected symbol near '{token.Content}'", token.Line, token.Column);
        }

        protected static void AssertTokenNext(LuaLexer lexer, params TokenType[] expected)
        {
            AssertToken(lexer.Current, expected);
            lexer.Next();
        }
    }
}
