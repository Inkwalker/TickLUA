using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class IfStatement : Statement
    {
        private Expression expression;
        private Statement mainStatement;
        private Statement elseStatement;

        private SourcePosition else_pos;

        public IfStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.If, TokenType.Elseif);

            expression = Expression.Create(lexer);

            AssertTokenNext(lexer, TokenType.Then);

            var main_compound = new CompoundStatement(lexer);
            mainStatement = new BlockStatement(main_compound);

            if (lexer.Current.Type == TokenType.Else)
            {
                else_pos = lexer.Current.Position;
                lexer.Next();

                var else_compound = new CompoundStatement(lexer);
                elseStatement = new BlockStatement(else_compound);
                AssertTokenNext(lexer, TokenType.End);
            }
            else if (lexer.Current.Type == TokenType.Elseif)
            {
                else_pos = lexer.Current.Position;
                elseStatement = new IfStatement(lexer);
            }
            else if (lexer.Current.Type == TokenType.End)
            {
                lexer.Next();
            }
            else 
                throw new CompilationException($"Unexpected symbol near {lexer.Current}. 'end' was expected", lexer.Current.Position);
            var end_pos = lexer.Current.Position;

            SourceRange = new SourceRange(start_pos, end_pos); 
        }

        public override void Compile(FunctionBuilder builder)
        {
            var context_result = expression.CompileReadAuto(builder);
            ushort line = (ushort)SourceRange.from.line;

            builder.AddInstruction(Instruction.TEST(context_result.index, false), line);
            int main_jump_addr = builder.AddInstruction(Instruction.NOP(), line); //placeholder for jump over main statement

            //expression is tested. We can reuse it's register
            builder.FreeRegisters(context_result);

            mainStatement.Compile(builder);

            int main_exit_addr = builder.InstructionCount - 1;

            if (elseStatement != null)
            {
                ushort else_line = (ushort)else_pos.line;
                main_exit_addr = builder.AddInstruction(Instruction.NOP(), else_line); // placeholder for jump over else statement
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
