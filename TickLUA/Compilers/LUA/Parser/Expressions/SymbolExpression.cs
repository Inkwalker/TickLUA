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
            name = lexer.Current.Content;
            lexer.Next();
        }

        // Compile as read operation
        public override byte CompileRead(FunctionBuilder builder)
        {
            ResultRegister = builder.ResolveVariable(name);
            return (byte)ResultRegister;
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

            builder.AddInstruction(Instruction.MOVE((byte)register, reg_value));
        }

        public override void ReleaseRegisters(FunctionBuilder builder)
        {
            // Symbols do not own their registers
            ResultRegister = -1;
        }
    }
}
