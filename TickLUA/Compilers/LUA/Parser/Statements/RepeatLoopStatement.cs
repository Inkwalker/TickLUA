using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class RepeatLoopStatement : Statement
    {
        private Expression condition;
        private CompoundStatement block;

        public RepeatLoopStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.Repeat);

            block = new CompoundStatement(lexer);

            AssertTokenNext(lexer, TokenType.Until);

            condition = Expression.Create(lexer);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);
        }


        public override void Compile(FunctionBuilder builder)
        {
            ushort line = (ushort)SourceRange.from.line;

            int addr_start = builder.InstructionCount;

            builder.BlockStart();

            block.Compile(builder);

            // until-condition, evaluated while the body's locals are still in scope
            var context_expr = condition.CompileReadAuto(builder);

            if (builder.BlockHasEscapingVars())
            {
                // A body local was captured by a closure. The captured cells must be closed
                // on BOTH paths: on loop-back so the next iteration is independent, and on
                // exit so the final closure keeps its own value once the registers get reused.
                // (CLOSE runs after the condition, which the linter's nil-CLOSE requires.)
                byte close_reg = builder.CurrentBlockOffset;

                builder.AddInstruction(Instruction.TEST(context_expr.index, true), line); // exit when true
                int addr_exit_jmp = builder.AddInstruction(Instruction.NOP(), line);      // JMP to exit-close
                builder.AddInstruction(Instruction.CLOSE(close_reg), line);               // loop path close
                int addr_back = builder.InstructionCount;
                builder.AddInstruction(Instruction.JMP(addr_start - addr_back - 1), line);
                int addr_exit = builder.InstructionCount;
                builder.SetInstruction(addr_exit_jmp, Instruction.JMP(addr_exit - addr_exit_jmp - 1));
                builder.AddInstruction(Instruction.CLOSE(close_reg), line);               // exit path close
            }
            else
            {
                // Loop back while the condition is false.
                builder.AddInstruction(Instruction.TEST(context_expr.index, false), line);
                int addr_loop_jmp = builder.InstructionCount;
                builder.AddInstruction(Instruction.JMP(addr_start - addr_loop_jmp - 1), line);
            }

            builder.FreeRegisters(context_expr);
            builder.BlockEnd();
        }
    }
}
