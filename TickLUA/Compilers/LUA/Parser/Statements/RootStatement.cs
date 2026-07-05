using System;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    /// <summary>
    /// Root statement of the chunk. Not a real statement. Wraps the code in a file.
    /// </summary>
    internal class RootStatement : Statement
    {
        private CompoundStatement body;

        public RootStatement(LuaLexer lexer)
        {
            var start = lexer.Current.Position;
            body = new CompoundStatement(lexer);
            var end = lexer.Current.Position;

            SourceRange = new SourceRange(start, end);
        }

        public override void Compile(FunctionBuilder builder)
        {
            builder.BlockStart();

            body.Compile(builder);

            builder.AddInstruction(Instruction.RETURN(0, -1), (ushort)SourceRange.to.line);

            builder.BlockEnd();
        }
    }
}
