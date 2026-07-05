using TickLUA.Compilers.LUA.Lexer;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class BreakStatement : Statement
    {
        public BreakStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.Break);
            SourceRange = new SourceRange(start_pos, start_pos);
        }

        public override void Compile(FunctionBuilder builder)
        {
            if (!builder.InLoop)
                throw new CompilationException("break outside a loop", SourceRange.from);

            builder.EmitBreak((ushort)SourceRange.from.line);
        }
    }
}
