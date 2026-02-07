using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class RepeatLoopStatement : Statement
    {
        private Expression condition;
        private BlockStatement block;

        public RepeatLoopStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.Repeat);

            block = new BlockStatement(lexer);

            AssertTokenNext(lexer, TokenType.Until);

            condition = Expression.Create(lexer);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);
        }


        public override void Compile(FunctionBuilder builder)
        {
            // condition expr
            byte reg_expr = builder.AllocateRegisters(1);
            int addr_start = builder.InstructionCount;

            block.Compile(builder);

            condition.CompileRead(builder, reg_expr);

            // condition test
            builder.AddInstruction(Instruction.TEST(reg_expr, false), (ushort)SourceRange.from.line);
            int addr_loop_jmp = builder.InstructionCount;
            // jump back
            builder.AddInstruction(Instruction.JMP(addr_start - addr_loop_jmp - 1), (ushort)SourceRange.from.line);

            builder.DeallocateRegisters(reg_expr);
        }
    }
}
