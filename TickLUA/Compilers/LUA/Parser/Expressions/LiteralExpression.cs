using System;
using System.Globalization;
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
                    Value = new NumberObject(ParseNumber(t));
                    break;
                //case TokenType.String:
                //    Value = new StringObject(t.Content);
                //    break;
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
                    throw new CompilationException("Literal type mismatch", t.Line, t.Column);
            }

            if (Value == null)
                throw new CompilationException($"Unknown literal format near '{t.Content}'",  t.Line, t.Column);

            lexer.Next();
        }

        private static float ParseNumber(Token t)
        {
            try
            {
                // Hex?
                if (t.Content.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    return (float)ParseLuaHexFloat(t.Content);
                }

                // Integer?
                if (int.TryParse(t.Content, out int i))
                {
                    return (float)i;
                }

                // Decimal float
                return float.Parse(t.Content, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new CompilationException("Literal type error", t.Line, t.Column);
            }
        }

        private static double ParseLuaHexFloat(string s)
        {
            // split "0x1.8p10"
            int p = s.IndexOf('p');
            string mantissaPart = p > -1 ? s.Substring(2, p - 2) : s;
            int exponent = p > -1 ? int.Parse(s.Substring(p + 1), CultureInfo.InvariantCulture) : 0;

            double mantissa = 0.0;

            // split integer and fractional hex parts
            var parts = mantissaPart.Split('.');
            mantissa += Convert.ToInt64(parts[0], 16);

            if (parts.Length == 2)
            {
                double frac = 0;
                double scale = 1.0 / 16;
                foreach (char c in parts[1])
                {
                    frac += HexDigit(c) * scale;
                    scale /= 16;
                }
                mantissa += frac;
            }

            return mantissa * Math.Pow(2, exponent);
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            throw new FormatException("Invalid hex digit: " + c);
        }

        public override byte CompileRead(FunctionBuilder builder)
        {
            ResultRegister = builder.AllocateRegisters(1);

            if (Value is BooleanObject boolObj)
            {
                // LOADBOOL
                builder.AddInstruction(Instruction.LOADBOOL((byte)ResultRegister, (bool)boolObj));
                return (byte)ResultRegister;
            }
            if (Value is NilObject)
            {
                // LOADNIL
                builder.AddInstruction(Instruction.LOADNIL((byte)ResultRegister));
                return (byte)ResultRegister;
            }

            // LOADK
            ushort index = builder.AddConstant(Value);
            builder.AddInstruction(Instruction.LOADK((byte)ResultRegister, index));

            return (byte)ResultRegister;
        }
    }
}
