using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    /// <summary>
    /// A group of statements that are executed in order.
    /// Intended to be used inside other statements, such as if, while, repeat, etc.
    /// Will not create a new block or close upvalues.
    /// </summary>
    internal class CompoundStatement : Statement
    {
        private List<Statement> statements;

        public CompoundStatement(LuaLexer lexer)
        {
            statements = new List<Statement>();

            var start_pos = lexer.Current.Position;

            while (true)
            {
                Token t = lexer.Current;
                if (t.IsEndOfBlock()) break;

                Statement s = Create(lexer);
                statements.Add(s);
            }

            var end_pos = lexer.Current.Position;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        public override void Compile(FunctionBuilder builder)
        {
            foreach (var statement in statements)
            {
                statement.Compile(builder);
            }
        }
    }
}
