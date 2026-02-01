using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class SymbolExpression : LValueExpression
    {
        private string name;

        public bool IsLocal { get; set; } = false;

        public SymbolExpression(string name)
        {
            this.name = name;
        }

        public SymbolExpression(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;

            name = lexer.Current.Content;
            lexer.Next();

            var end_pos = start_pos + name.Length;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        // Compile as read operation
        public override void CompileRead(FunctionBuilder builder, byte reg_result)
        {
            int reg_var = builder.ResolveVariable(name);

            // We always copy from a variable register to eliminate accidental overwrites with temp values.
            builder.AddInstruction(Instruction.MOVE(reg_result, (byte)reg_var), (ushort)SourceRange.from.line);
        }

        public override void CompileWrite(FunctionBuilder builder, byte reg_value)
        {
            int register;
            if (IsLocal)
                register = builder.AllocateVariable(name);
            else
                register = builder.ResolveVariable(name);

            if (register == -1)
                throw new CompilationException($"Undefined variable '{name}'", 1, 1); //TODO: line/column

            ushort line = (ushort)SourceRange.from.line;

            builder.AddInstruction(Instruction.MOVE((byte)register, reg_value), line);
        }
    }
}
