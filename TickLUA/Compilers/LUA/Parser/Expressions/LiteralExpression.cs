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
            Value = new IntegerObject(value);
        }

        //public LiteralExpression(float value)
        //{
        //    Value = new NumberObject(value);
        //}

        //public LiteralExpression(bool value)
        //{
        //    Value = new BooleanObject(value);
        //}

        //public LiteralExpression(string value)
        //{
        //    Value = new StringObject(value);
        //}

        public LiteralExpression(LuaObject obj)
        {
            Value = obj;
        }

        public LiteralExpression(LuaLexer lexer)
        {
            var t = lexer.Current;

            switch (t.Type)
            {
                case TokenType.Number:
                    Value = new IntegerObject(int.Parse(t.Content));
                    break;
                //case TokenType.String:
                //    Value = new StringObject(t.Content);
                //    break;
                //case TokenType.True:
                //    Value = BooleanObject.True;
                //    break;
                //case TokenType.False:
                //    Value = BooleanObject.False;
                //    break;
                case TokenType.Nil:
                    Value = NilObject.Nil;
                    break;
                default:
                    throw new CompilationException("Literal type mismatch", t.Line, t.Column);
            }

            if (Value == null)
                throw new CompilationException($"Unknown literal format near '{t.Content}'",  t.Line, t.Column);

            lexer.Next();
        }

        public override byte CompileRead(FunctionBuilder builder)
        {
            ushort index = builder.AddConstant(Value);
            ResultRegister = builder.AllocateRegisters(1);
            builder.AddInstruction(Instruction.LOADK((byte)ResultRegister, index));

            return (byte)ResultRegister;
        }
    }
}
