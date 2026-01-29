using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class IfStatement : Statement
    {
        private Expression expression;
        private Statement mainStatement;
        private Statement elseStatement;

        public IfStatement(LuaLexer lexer)
        {
            AssertTokenNext(lexer, TokenType.If, TokenType.Elseif);

            expression = Expression.Create(lexer);

            AssertTokenNext(lexer, TokenType.Then);

            mainStatement = new BlockStatement(lexer);

            if (lexer.Current.Type == TokenType.Else)
            {
                lexer.Next();
                elseStatement = new BlockStatement(lexer);
                AssertTokenNext(lexer, TokenType.End);
            }
            else if (lexer.Current.Type == TokenType.Elseif)
            {
                elseStatement = new IfStatement(lexer);
            }
            else if (lexer.Current.Type == TokenType.End)
            {
                lexer.Next();
            }
            else 
                throw new CompilationException($"Unexpected symbol near {lexer.Current}. 'end' was expected", lexer.Current.Line, lexer.Current.Column);
        }

        public override void Compile(FunctionBuilder builder)
        {
            byte reg_result = expression.CompileRead(builder);

            builder.AddInstruction(Instruction.TEST(reg_result, false));
            int main_jump_addr = builder.AddInstruction(Instruction.NOP()); //placeholder for jump over main statement

            expression.ReleaseRegisters(builder); //expression is tested. We can reuse it's registers
            mainStatement.Compile(builder);

            int main_exit_addr = builder.InstructionCount - 1;

            if (elseStatement != null)
            {
                main_exit_addr = builder.AddInstruction(Instruction.NOP()); // placeholder for jump over else statement
                // end of main statement

                elseStatement.Compile(builder);

                int else_exit_addr = builder.InstructionCount - 1;

                //Jump over the else statement at the end of the main statement
                int else_jump_offset = else_exit_addr - main_exit_addr;
                builder.SetInstruction(main_exit_addr, Instruction.JMP(else_jump_offset));
            }

            //Jump over the main statement if false
            int main_jump_offset = main_exit_addr - main_jump_addr;
            builder.SetInstruction(main_jump_addr, Instruction.JMP(main_jump_offset));
        }
    }
}
