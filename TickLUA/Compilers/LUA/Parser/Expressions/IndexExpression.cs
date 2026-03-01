using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class IndexExpression : LValueExpression
    {
        private Expression variable;
        private Expression index;

        public IndexExpression(Expression variable, string index)
        {
            this.variable = variable;
            this.index = new LiteralExpression(index);
        }

        public IndexExpression(Expression variable, Expression index)
        {
            this.variable = variable;
            this.index = index;

            SourceRange = new SourceRange(variable.SourceRange.from, index.SourceRange.to);
        }

        //public static IndexExpression Env(string index)
        //{
        //    var variable = new SymbolExpression(LuaObject.ENV);
        //    return new IndexExpression(variable, index);
        //}

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            var context_table = variable.CompileReadAuto(builder);
            var context_key   = index.CompileReadAuto(builder);

            builder.AddInstruction(Instruction.GET_TABLE(target_register.index, context_table.index, context_key.index), (ushort)SourceRange.from.line);
            
            builder.FreeRegisters(context_key);
            builder.FreeRegisters(context_table);
        }

        public override void CompileWrite(FunctionBuilder builder, RegisterContext value_register)
        {
            var context_table = variable.CompileReadAuto(builder);
            var context_key   = index.CompileReadAuto(builder);

            builder.AddInstruction(Instruction.SET_TABLE(context_table.index, context_key.index, value_register.index), (ushort)SourceRange.from.line);

            builder.FreeRegisters(context_key);
            builder.FreeRegisters(context_table);
        }

        public override byte PreallocateRegister(FunctionBuilder builder)
        {
            throw new CompilationException("Cannot preallocate register for index expression", SourceRange.from);
        }
    }
}
