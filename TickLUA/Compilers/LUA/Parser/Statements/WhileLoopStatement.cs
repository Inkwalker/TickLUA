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

            var body_compound = new CompoundStatement(lexer);
            block = new BlockStatement(body_compound);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);

            lexer.Next(); // end
        }


        public override void Compile(FunctionBuilder builder)
        {
            builder.LoopStart();

            // condition expr
            int addr_start = builder.InstructionCount;

            var context_expr = condition.CompileReadAuto(builder);

            // condition test
            builder.AddInstruction(Instruction.TEST(context_expr.index, false), (ushort)SourceRange.from.line);
            int addr_exit_jmp = builder.AddInstruction(Instruction.NOP(), (ushort)SourceRange.from.line); // exit jump placeholder
            
            builder.FreeRegisters(context_expr);

            // body
            block.Compile(builder);

            // jump back
            int addr_loop_jmp = builder.InstructionCount;
            builder.AddInstruction(Instruction.JMP(addr_start - addr_loop_jmp - 1), (ushort)SourceRange.to.line);
            int addr_exit = builder.InstructionCount;

            // set exit jump
            builder.SetInstruction(addr_exit_jmp, Instruction.JMP(addr_exit - addr_exit_jmp - 1));

            builder.LoopEnd();
        }
    }
}
