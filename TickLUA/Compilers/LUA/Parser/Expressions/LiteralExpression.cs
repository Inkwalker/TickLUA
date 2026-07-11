using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class LiteralExpression : Expression
    {
        public LuaObject Value { get; }

        public LiteralExpression(int value)
        {
            Value = new NumberObject(value);
        }

        public LiteralExpression(float value)
        {
            Value = new NumberObject(value);
        }

        public LiteralExpression(bool value)
        {
            Value = BooleanObject.FromBool(value);
        }

        public LiteralExpression(string value)
        {
            Value = new StringObject(value);
        }

        public LiteralExpression(LuaObject obj)
        {
            Value = obj;
        }

        public LiteralExpression(LuaLexer lexer)
        {
            var t = lexer.Current;
            var start_pos = t.Position;

            switch (t.Type)
            {
                case TokenType.Number:
                    Value = new NumberObject(ParseNumber(t));
                    break;
                case TokenType.String:
                    Value = new StringObject(t.Content);
                    break;
                case TokenType.True:
                    Value = BooleanObject.True;
                    break;
                case TokenType.False:
                    Value = BooleanObject.False;
                    break;
                case TokenType.Nil:
                    Value = NilObject.Nil;
                    break;
                default:
                    throw new CompilationException("Literal type mismatch", t.Position);
            }

            if (Value == null)
                throw new CompilationException($"Unknown literal format near '{t.Content}'", t.Position);

            lexer.Next();

            var end_pos = start_pos + t.Content.Length;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        private static float ParseNumber(Token t)
        {
            if (NumberObject.TryParse(t.Content, out var number))
                return number.Value;

            throw new CompilationException("Literal type error", t.Position);
        }

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            ushort line = (ushort)SourceRange.from.line;

            if (Value is BooleanObject boolObj)
            {
                // LOAD_BOOL
                builder.AddInstruction(Instruction.LOAD_BOOL(target_register.index, (bool)boolObj), line);
                return;
            }
            if (Value is NilObject)
            {
                // LOAD_NIL
                builder.AddInstruction(Instruction.LOAD_NIL(target_register.index), line);
                return;
            }

            // Optimization for small integers
            if (Value is NumberObject numObj && numObj.IsInteger)
            {
                int intValue = (int)numObj;
                if (intValue >= short.MinValue && intValue <= short.MaxValue)
                {
                    // LOAD_INT
                    builder.AddInstruction(Instruction.LOAD_INT(target_register.index, (short)intValue), line);
                    return;
                }
            }

            // LOAD_CONST
            ushort index = builder.AddConstant(Value);
            builder.AddInstruction(Instruction.LOAD_CONST(target_register.index, index), line);
        }
    }
}
