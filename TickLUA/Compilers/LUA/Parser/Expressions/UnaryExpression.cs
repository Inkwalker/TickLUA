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

            var start_pos = token.Position;
            var end_pos = expression.SourceRange.to;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        public override void CompileRead(FunctionBuilder builder, byte reg_result)
        {
            expression.CompileRead(builder, reg_result);

            if (operation != OperationType.None)
            {
                ushort line = (ushort)SourceRange.from.line;

                switch (operation)
                {
                    case OperationType.Negate:
                        builder.AddInstruction(Instruction.UNM(reg_result, reg_result), line);
                        break;
                    case OperationType.Not:
                        builder.AddInstruction(Instruction.NOT(reg_result, reg_result), line);
                        break;
                    case OperationType.Len:
                        throw new CompilationException($"Not implemented binary operator '#'", line, 1);
                    default:
                        throw new CompilationException($"Unexpected unary operator", line, 1);
                }
            }
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
