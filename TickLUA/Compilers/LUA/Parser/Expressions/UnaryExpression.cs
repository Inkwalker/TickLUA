using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class UnaryExpression : Expression
    {
        private Expression expression;
        private OperationType operation;

        public UnaryExpression(Expression expression, Token token, LuaLexer lexer)
        {
            this.expression = expression;
            this.operation = ParseOperation(token);

            //optimization for number literals
            if (operation == OperationType.Negate && expression is LiteralExpression le && le.Value is NumberObject num)
            {
                this.expression = new LiteralExpression(-num.Value);
                this.operation = OperationType.None;
            }
        }

        public override byte CompileRead(FunctionBuilder builder)
        {
            byte reg_val = expression.CompileRead(builder);

            if (operation == OperationType.None)
                ResultRegister = reg_val;
            else
            {
                // we don't know if it's named register or not
                // so allocate a new one that can be deallocated safely
                ResultRegister = builder.AllocateRegisters(1);

                switch (operation)
                {
                    case OperationType.Negate:
                        builder.AddInstruction(Instruction.UNM((byte)ResultRegister, reg_val));
                        break;
                    case OperationType.Not:
                        builder.AddInstruction(Instruction.NOT((byte)ResultRegister, reg_val));
                        break;
                    case OperationType.Len:
                        throw new CompilationException($"Not implemented binary operator '#'", 1, 1);
                    default:
                        throw new CompilationException($"Unexpected unary operator", 1, 1);
                }
            }

            return (byte)ResultRegister;
        }

        public override void ReleaseRegisters(FunctionBuilder builder)
        {
            if (ResultRegister > -1)
            {
                // None operation doesn't allocate
                if (operation != OperationType.None) 
                    builder.DeallocateRegisters((byte)ResultRegister, 1);

                ResultRegister = -1;
            }
            expression.ReleaseRegisters(builder);
        }

        private static OperationType ParseOperation(Token token)
        {
            switch (token.Type)
            {
                case TokenType.OP_Sub: return OperationType.Negate;
                case TokenType.Not: return OperationType.Not;
                case TokenType.OP_Len: return OperationType.Len;
                default: return OperationType.None;
            }
        }

        public enum OperationType
        {
            None,
            Negate,
            Not,
            Len
        }
    }
}
