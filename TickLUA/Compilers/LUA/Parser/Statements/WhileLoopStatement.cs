using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class WhileLoopStatement : Statement
    {
        private Expression condition;
        private BlockStatement block;

        public WhileLoopStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.While);

            condition = Expression.Create(lexer);

            AssertTokenNext(lexer, TokenType.Do);

            block = new BlockStatement(lexer);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);

            lexer.Next(); // end
        }


        public override void Compile(FunctionBuilder builder)
        {
            // condition expr
            byte reg_expr = builder.AllocateRegisters(1);
            int addr_start = builder.InstructionCount;

            condition.CompileRead(builder, reg_expr);

            // condition test
            builder.AddInstruction(Instruction.TEST(reg_expr, false), (ushort)SourceRange.from.line);
            int addr_exit_jmp = builder.AddInstruction(Instruction.NOP(), (ushort)SourceRange.from.line); // exit jump placeholder

            // body
            block.Compile(builder);

            // jump back
            int addr_loop_jmp = builder.InstructionCount;
            builder.AddInstruction(Instruction.JMP(addr_start - addr_loop_jmp - 1), (ushort)SourceRange.to.line);
            int addr_exit = builder.InstructionCount;

            // set exit jump
            builder.SetInstruction(addr_exit_jmp, Instruction.JMP(addr_exit - addr_exit_jmp - 1));

            builder.DeallocateRegisters(reg_expr);
        }
    }
}
