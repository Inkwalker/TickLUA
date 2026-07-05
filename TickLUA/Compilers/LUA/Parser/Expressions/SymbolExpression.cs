using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class SymbolExpression : LValueExpression
    {
        private string name;

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
        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            ushort line = (ushort)SourceRange.from.line;
            int reg_var = builder.ResolveVariable(name);

            if (reg_var >= 0)
            {
                // local variable, just move it to the result register
                builder.AddInstruction(Instruction.MOVE(target_register.index, (byte)reg_var), line);
            }
            else
            {
                // try to resolve as upvalue
                int upval_index = builder.ResolveUpValue(name);
                if (upval_index >= 0)
                {
                    builder.AddInstruction(Instruction.GET_UPVAL(target_register.index, (byte)upval_index), line);
                }
                else
                {
                    // resolve as global variable
                    throw new CompilationException($"Undefined variable '{name}'", SourceRange.from);
                }
            }
        }

        public override void CompileWrite(FunctionBuilder builder, RegisterContext value_register)
        {
            ushort line = (ushort)SourceRange.from.line;

            int reg_var = builder.ResolveVariable(name);

            if (reg_var >= 0)
            {
                // local variable, just move the value to it
                builder.AddInstruction(Instruction.MOVE((byte)reg_var, value_register.index), line);
            }
            else
            {
                // try to resolve as upvalue
                int upval_index = builder.ResolveUpValue(name);
                if (upval_index >= 0)
                {
                    builder.AddInstruction(Instruction.SET_UPVAL(value_register.index, (byte)upval_index), line);
                    return;
                }
                else
                {
                    // resolve as global variable
                    throw new CompilationException($"Undefined variable '{name}'", SourceRange.from);
                }
            }
        }

        public override byte PreallocateRegister(FunctionBuilder builder)
        {
            return builder.AllocateVariable(name);
        }
    }
}
