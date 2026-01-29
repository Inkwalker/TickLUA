using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class BlockStatement : Statement
    {
        private List<Statement> statements;

        public BlockStatement(LuaLexer lexer)
        {
            statements = new List<Statement>();

            while (true)
            {
                Token t = lexer.Current;
                if (t.IsEndOfBlock()) break;

                Statement s = Create(lexer);
                statements.Add(s);
            }
        }

        public override void Compile(FunctionBuilder builder)
        {
            builder.BlockStart();

            foreach (var statement in statements)
            {
                statement.Compile(builder);
            }

            builder.BlockEnd();
        }
    }
}
