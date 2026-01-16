using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class ReturnStatement : Statement
    {
        private List<Expression> values;

        private int line;

        public ReturnStatement(LuaLexer lexer)
        {
            line = lexer.Current.Line;
            AssertTokenNext(lexer, TokenType.Return);

            values = new List<Expression>();
            while (!lexer.Current.IsEndOfBlock() && lexer.Current.Type != TokenType.Semicolon)
            {
                var e = Expression.Create(lexer);

                values.Add(e);

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }
        }

        public override void Compile(FunctionBuilder builder)
        {
            if (values.Count == 0)
            {
                // Return nothing
                builder.AddInstruction(Instruction.RETURN(0, 0));
                return;
            }

            List<byte> regs = new List<byte>(values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                byte reg = values[i].CompileRead(builder);
                regs.Add(reg);
            }

            if (!IsConsecutiveRegisters(regs))
            {
                var new_start_reg = builder.AllocateRegisters(regs.Count);
                for (int i = 0; i < regs.Count; i++)
                {
                    byte new_reg = (byte)(new_start_reg + i);
                    builder.AddInstruction(Instruction.MOVE(new_reg, regs[i]));
                    regs[i] = new_reg;
                }
            }

            builder.AddInstruction(Instruction.RETURN(regs[0], regs.Count));
        }

        private static bool IsConsecutiveRegisters(List<byte> regs)
        {
            for (int i = 1; i < regs.Count; i++)
            {
                if (regs[i] != regs[i - 1] + 1) return false;
            }
            return true;
        }
    }
}
