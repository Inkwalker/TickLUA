using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class BlockStatement : Statement
    {
        private CompoundStatement body;

        public BlockStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;

            body = new CompoundStatement(lexer);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);
        }

        public BlockStatement(CompoundStatement body)
        {
            this.body = body;
            SourceRange = body.SourceRange;
        }

        public override void Compile(FunctionBuilder builder)
        {
            builder.BlockStart();

            body.Compile(builder);

            // Close upvalues if there any
            if (builder.BlockHasEscapingVars())
            {
                builder.AddInstruction(Instruction.CLOSE(builder.CurrentBlockOffset), (ushort)SourceRange.to.line);
            }

            builder.BlockEnd();
        }
    }
}
